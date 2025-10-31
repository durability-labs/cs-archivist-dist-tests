using ArchivistClient;
using ArchivistContractsPlugin;
using ArchivistContractsPlugin.ChainMonitor;
using ArchivistContractsPlugin.Marketplace;
using ArchivistReleaseTests.Utils;
using Nethereum.Hex.HexConvertors.Extensions;
using NUnit.Framework;
using Utils;

namespace ArchivistReleaseTests.Repair
{
    [TestFixture]
    public class SlotRepairTest : MarketplaceAutoBootstrapDistTest
    {
        #region Setup

        private readonly PurchaseParams purchaseParams = new PurchaseParams(
            nodes: 4,
            tolerance: 2,
            uploadFilesize: 32.MB()
        );

        public SlotRepairTest()
        {
            Assert.That(purchaseParams.Nodes, Is.LessThan(NumberOfHosts));
        }

        protected override int NumberOfHosts => 6;
        protected override int NumberOfClients => 1;
        protected override ByteSize HostAvailabilitySize => purchaseParams.SlotSize.Multiply(1.1); // Each host can hold 1 slot.
        protected override TimeSpan HostAvailabilityMaxDuration => TimeSpan.FromDays(5.0);
        
        #endregion

        private int proofsMissed = 0;

        [Test]
        [Combinatorial]
        public void RollingRepairSingleFailure(
            [Rerun] int rerun,
            [Values(10)] int numFailures)
        {
            Assert.That(numFailures, Is.GreaterThan(NumberOfHosts));

            var hosts = StartHosts().ToList();
            var client = StartClients().Single();
            StartValidator();

            proofsMissed = 0;
            var contract = CreateStorageRequest(client);
            contract.WaitForStorageContractStarted();
            // All slots are filled.

            client.Stop(waitTillStopped: true);

            // Hold this situation 
            Log("Holding initial situation to ensure contract is stable...");
            var config = GetContracts().Deployment.Config;
            WaitAndCheckNodesStaysAlive(config.PeriodDuration * 5, hosts);

            // No proofs were missed so far.
            Assert.That(proofsMissed, Is.EqualTo(0), $"Proofs were missed *BEFORE* any hosts were shut down.");
            
            var requestState = GetContracts().GetRequestState(contract.PurchaseId.HexToByteArray());
            Assert.That(requestState, Is.Not.EqualTo(RequestState.Failed));

            for (var i = 0; i < numFailures; i++)
            {
                var fills = GetOnChainSlotFills(hosts);

                Log($"Failure step: {i}");
                Log($"Running hosts: [{string.Join(", ", hosts.Select(h => h.GetName()))}]");
                Log($"Current fills: {string.Join(", ", fills.Select(f => f.ToString()))}");

                // Start a new host. Add it to the back of the list:
                hosts.Add(StartOneHost());

                // Pick a filled slot by a host at or near the front of the list.
                var fill = GetSlotFillByOldestHost(fills, hosts);

                Log($"Causing failure for host: {fill.Host.GetName()} slotIndex: {fill.SlotFilledEvent.SlotIndex}");
                hosts.Remove(fill.Host);
                fill.Host.Stop(waitTillStopped: true);

                // The slot should become free.
                WaitForSlotFreedEvent(contract, fill.SlotFilledEvent.SlotIndex);

                // One of the other hosts should pick up the free slot.
                WaitForNewSlotFilledEvent(contract, fill.SlotFilledEvent.SlotIndex);
            }
        }

        protected override void OnPeriod(PeriodReport report)
        {
            proofsMissed += report.GetNumberOfProofsMissed();

            // There can't be any calls to FreeSlot.
            // We expect the validator to call MarkProofAsMissing
            // which should result in SlotFreed events.
            foreach (var c in report.FunctionCalls)
            {
                Assert.That(c.Name, Is.Not.EqualTo(nameof(FreeSlot1Function)));
                Assert.That(c.Name, Is.Not.EqualTo(nameof(FreeSlotFunction)));
            }
        }

        private void WaitForSlotFreedEvent(IStoragePurchaseContract contract, ulong slotIndex)
        {
            var timeout = CalculateContractFailTimespan();
            var context = $"(requestId: '{contract.PurchaseId.ToLowerInvariant()}' slotIndex: {slotIndex}) - ";
            Log($"{context} Timeout: {Time.FormatDuration(timeout)}");

            WaitForNewEventWithTimeout(
                timeout: timeout,
                waiter: () => GetContracts().WaitUntilNextPeriod(),
                checker: events =>
                {
                    var slotsFreed = events.GetEvents<SlotFreedEventDTO>();
                    Log($"{context} Slots freed this period: {slotsFreed.Length}");

                    foreach (var free in slotsFreed)
                    {
                        var freedId = free.RequestId.ToHex().ToLowerInvariant();
                        Log($"{context} {free.Block} Free for requestId '{freedId}' slotIndex: {free.SlotIndex}");

                        if (freedId.Equals(contract.PurchaseId, StringComparison.InvariantCultureIgnoreCase))
                        {
                            if (free.SlotIndex == slotIndex)
                            {
                                Log($"{context} {free.Block} Done: found the correct slotFree event.");
                                return true;
                            }
                        }
                    }
                    return false;
                });
        }

        private void WaitForNewSlotFilledEvent(IStoragePurchaseContract contract, ulong slotIndex)
        {
            var timeout = contract.Purchase.Expiry;
            var context = $"(requestId: '{contract.PurchaseId.ToLowerInvariant()}' slotIndex: {slotIndex}) - ";
            Log($"{context} Timeout: {Time.FormatDuration(timeout)}");

            WaitForNewEventWithTimeout(
                timeout: timeout,
                waiter: () => Thread.Sleep(TimeSpan.FromSeconds(15)),
                checker: events =>
                {
                    var slotFillEvents = events.GetEvents<SlotFilledEventDTO>();
                    Log($"{context} Slots filled in last 15 seconds: {slotFillEvents.Length}");

                    var matches = slotFillEvents.Where(f =>
                    {
                        return
                            f.RequestId.ToHex().ToLowerInvariant() == contract.PurchaseId.ToLowerInvariant() &&
                            f.SlotIndex == slotIndex;
                    }).ToArray();

                    if (matches.Length > 1)
                    {
                        var msg = string.Join(",", matches.Select(f => f.ToString()));
                        Assert.Fail($"{context} Somehow, the slot got filled multiple times: {msg}");
                    }
                    if (matches.Length == 1)
                    {
                        Log($"{context} Found the correct new slotFilled event: {matches[0].ToString()}");
                        return true;
                    }
                    return false;
                });
        }

        private void WaitForNewEventWithTimeout(TimeSpan timeout, Action waiter, Func<IArchivistContractsEvents, bool> checker)
        {
            var start = DateTime.UtcNow;
            var loop = start;
            Thread.Sleep(TimeSpan.FromSeconds(3.0));

            while (DateTime.UtcNow < start + timeout)
            {
                var loopStart = loop;
                loop = DateTime.UtcNow;
                var events = GetContracts().GetEvents(new TimeRange(loopStart, loop));

                if (checker(events)) return;
                waiter();
            }
            Assert.Fail($"{nameof(WaitForNewEventWithTimeout)} Failed after {Time.FormatDuration(timeout)}");
        }

        private SlotFill GetSlotFillByOldestHost(SlotFill[] fills, List<IArchivistNode> hosts)
        {
            var copy = hosts.ToArray();
            foreach (var host in copy)
            {
                var fill = GetFillByHost(host, fills);
                if (fill == null)
                {
                    // This host didn't fill anything.
                    // Move this one to the back of the list.
                    hosts.Remove(host);
                    hosts.Add(host);
                }
                else
                {
                    return fill;
                }
            }
            throw new Exception("None of the hosts seem to have filled a slot.");
        }

        private SlotFill? GetFillByHost(IArchivistNode host, SlotFill[] fills)
        {
            // If these is more than 1 fill by this host, the test is misconfigured.
            // The availability size of the host should guarantee it can fill 1 slot maximum.
            return fills.SingleOrDefault(f => f.Host.EthAddress == host.EthAddress);
        }

        private IStoragePurchaseContract CreateStorageRequest(IArchivistNode client)
        {
            var cid = client.UploadFile(GenerateTestFile(purchaseParams.UploadFilesize));
            var config = GetContracts().Deployment.Config;
            return client.Marketplace.RequestStorage(new StoragePurchaseRequest(cid)
            {
                Duration = HostAvailabilityMaxDuration / 2,
                MinRequiredNumberOfNodes = (uint)purchaseParams.Nodes,
                NodeFailureTolerance = (uint)purchaseParams.Tolerance,
                ProofProbability = 1, // One proof every period. Free slot as quickly as possible.
            });
        }
    }
}
