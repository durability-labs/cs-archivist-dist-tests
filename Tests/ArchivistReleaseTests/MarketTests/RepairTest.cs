using ArchivistClient;
using ArchivistContractsPlugin;
using ArchivistContractsPlugin.ChainMonitor;
using ArchivistContractsPlugin.Marketplace;
using ArchivistReleaseTests.Utils;
using Nethereum.Hex.HexConvertors.Extensions;
using NUnit.Framework;
using Utils;

namespace ArchivistReleaseTests.MarketTests
{
    [TestFixture]
    public class RepairTest : MarketplaceAutoBootstrapDistTest
    {
        #region Setup

        private readonly PurchaseParams purchaseParams = new PurchaseParams(
            nodes: 4,
            tolerance: 2,
            uploadFilesize: 32.MB()
        );

        public RepairTest()
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
            Assert.That(proofsMissed, Is.EqualTo(0), "Proofs were missed *BEFORE* any hosts were shut down.");
            
            var requestState = GetContracts().GetRequestState(contract.PurchaseId.HexToByteArray());
            Assert.That(requestState, Is.Not.EqualTo(RequestState.Failed));

            for (var i = 0; i < numFailures; i++)
            {
                Log($"Failure step: {i}");
                Log($"Running hosts: [{string.Join(", ", hosts.Select(h => h.GetName()))}]");

                // Start a new host. Add it to the back of the list:
                hosts.Add(StartOneHost());

                var fill = GetSlotFillByOldestHost(hosts);

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
            var start = DateTime.UtcNow;
            var timeout = CalculateContractFailTimespan();

            Log($"{nameof(WaitForSlotFreedEvent)} {Time.FormatDuration(timeout)} requestId: '{contract.PurchaseId.ToLowerInvariant()}' slotIndex: {slotIndex}");

            while (DateTime.UtcNow < start + timeout)
            {
                var events = GetContracts().GetEvents(GetTestRunTimeRange());
                var slotsFreed = events.GetEvents<SlotFreedEventDTO>();
                Log($"Slots freed this period: {slotsFreed.Length}");

                foreach (var free in slotsFreed)
                {
                    var freedId = free.RequestId.ToHex().ToLowerInvariant();
                    Log($"Free for requestId '{freedId}' slotIndex: {free.SlotIndex}");

                    if (freedId == contract.PurchaseId.ToLowerInvariant())
                    {
                        if (free.SlotIndex == slotIndex)
                        {
                            Log("Found the correct slotFree event");
                            return;
                        }
                    }
                }

                GetContracts().WaitUntilNextPeriod();
            }
            Assert.Fail($"{nameof(WaitForSlotFreedEvent)} for contract {contract.PurchaseId} and slotIndex {slotIndex} failed after {Time.FormatDuration(timeout)}");
        }

        private void WaitForNewSlotFilledEvent(IStoragePurchaseContract contract, ulong slotIndex)
        {
            Log(nameof(WaitForNewSlotFilledEvent));
            var start = DateTime.UtcNow - TimeSpan.FromSeconds(10.0);
            var timeout = contract.Purchase.Expiry;

            while (DateTime.UtcNow < start + timeout)
            {
                var newTimeRange = new TimeRange(start, DateTime.UtcNow); // We only want to see new fill events.
                var events = GetContracts().GetEvents(newTimeRange);
                var slotFillEvents = events.GetEvents<SlotFilledEventDTO>();

                var matches = slotFillEvents.Where(f =>
                {
                    return
                        f.RequestId.ToHex().ToLowerInvariant() == contract.PurchaseId.ToLowerInvariant() &&
                        f.SlotIndex == slotIndex;
                }).ToArray();

                if (matches.Length > 1)
                {
                    var msg = string.Join(",", matches.Select(f => f.ToString()));
                    Assert.Fail($"Somehow, the slot got filled multiple times: {msg}");
                }
                if (matches.Length == 1)
                {
                    Log($"Found the correct new slotFilled event: {matches[0].ToString()}");
                    return;
                }

                Thread.Sleep(TimeSpan.FromSeconds(15));
            }
            Assert.Fail($"{nameof(WaitForSlotFreedEvent)} for contract {contract.PurchaseId} and slotIndex {slotIndex} failed after {Time.FormatDuration(timeout)}");
        }

        private SlotFill GetSlotFillByOldestHost(List<IArchivistNode> hosts)
        {
            var fills = GetOnChainSlotFills(hosts);
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
