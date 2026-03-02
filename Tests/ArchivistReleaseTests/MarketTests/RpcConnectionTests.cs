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
        public void Start()
        {
            var (hosts, clients) = JumpStartHostsAndClients();
            var client = clients.Single();

            var request = CreateStorageRequest(client);
            request.WaitForStorageContractStarted();

            var fills = GetOnChainSlotFills(hosts);
            // We select one host that has filled at least one slot.
            var host = fills.First().Host;
            var slots = host.Marketplace.GetSlots();
            // We select one slot of this host.
            var slot = slots.First();
            var slotId = slot.Id;

            Assert.That(host.Marketplace.GetSlot(slotId).State, Is.EqualTo(StorageSlotState.Proving));

            // The RPC connection provider goes down.
            var rpcNode = GetGeth();
            rpcNode.Pause();

            // We expect the host to report and error state for this slot.
            WaitUntilSlotState(host, slotId, StorageSlotState.Errored);

            // We resume the RPC, and the host's slot should recover.
            rpcNode.Resume();

            WaitUntilSlotState(host, slotId, StorageSlotState.Proving);
        }

        private void WaitUntilSlotState(IArchivistNode host, string slotId, StorageSlotState state)
        {
            Time.WaitUntil(() =>
                host.Marketplace.GetSlot(slotId).State == state,
                timeout: GetPeriodDuration() * 2,
                retryDelay: TimeSpan.FromSeconds(10),
                msg: $"{nameof(WaitUntilSlotState)} == {state}"
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
