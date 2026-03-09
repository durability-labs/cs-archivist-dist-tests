using ArchivistClient;
using ArchivistReleaseTests.Utils;
using NUnit.Framework;
using Utils;

namespace ArchivistReleaseTests.MarketTests
{
    [TestFixture]
    public class RpcConnectionTests : MarketplaceAutoBootstrapDistTest
    {
        protected override int NumberOfHosts => 4;
        protected override int NumberOfClients => 1;

        [Test]
        public void RecoversSlotAfterDisconnect()
        {
            // Unusual setup: We're using an extra geth node that follows the miner.
            // We can stop and restart it, disconnecting archivist host node
            // without screwing with the testing infra that's monitoring the chain.

            var rpcNode = StartGethNode(s => s.WithName("follower").WithBootstrapNode(GetGeth()));
            var hosts = StartHosts(s => s
                    .EnableMarketplace(rpcNode, GetContracts(), s => s
                    .WithInitial(StartingBalanceEth.Eth(), HostStartingBalance)
                    .AsStorageNode()));
            var client = StartClients().Single();

            var request = CreateStorageRequest(client);
            request.WaitForStorageContractStarted();

            var fills = GetOnChainSlotFills(hosts);
            // We select one host that has filled at least one slot.
            var host = fills.First().Host;
            var slots = host.Marketplace.GetSlots();
            // We select one slot of this host.
            var slot = slots.First();
            var slotId = slot.Id;

            Log("The state of a successfully started slot should be 'proving'.");
            Assert.That(host.Marketplace.GetSlot(slotId).State, Is.EqualTo(StorageSlotState.Proving));

            Log("The RPC connection provider goes down.");
            rpcNode.Pause();

            Log("We expect the host to report and error state for this slot.");
            WaitUntilSlotState(host, slotId, StorageSlotState.Errored);

            Log("We restore the RPC connecting...");
            rpcNode.Resume();

            Log("The host's slot should recover.");
            WaitUntilSlotState(host, slotId, StorageSlotState.Proving);
        }

        private void WaitUntilSlotState(IArchivistNode host, string slotId, StorageSlotState expected)
        {
            Log($"{expected}");
            Time.WaitUntil(() =>
                {
                    try
                    {
                        var actual = host.Marketplace.GetSlot(slotId).State;
                        return actual == expected;
                    }
                    catch (TimeoutException timeout)
                    {
                        if (timeout.InnerException is AggregateException agg1)
                        {
                            if (agg1.InnerException is AggregateException agg2)
                            {
                                if (agg2.InnerException is ArchivistOpenApi.ApiException apiException)
                                {
                                    return 
                                        expected == StorageSlotState.Errored &&
                                        apiException.Message.Contains("Host is not in an active sale for the slot");
                                }
                            }
                        }
                        return false;
                    }
                },
                timeout: GetPeriodDuration() * 2,
                retryDelay: TimeSpan.FromSeconds(10),
                msg: $"{nameof(WaitUntilSlotState)} == {expected}"
            );
        }

        private IStoragePurchaseContract CreateStorageRequest(IArchivistNode client)
        {
            var cid = client.UploadFile(GenerateTestFile(DefaultPurchase.UploadFilesize));
            return client.Marketplace.RequestStorage(new StoragePurchaseRequest(cid)
            {
                Duration = HostAvailabilityMaxDuration * 0.75
            });
        }
    }
}
