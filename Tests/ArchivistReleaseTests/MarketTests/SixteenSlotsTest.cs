using ArchivistClient;
using ArchivistReleaseTests.Utils;
using NUnit.Framework;
using Utils;

namespace ArchivistReleaseTests.MarketTests
{
    public class SixteenSlotsTest : MarketplaceAutoBootstrapDistTest
    {
        #region Setup

        private readonly PurchaseParams purchaseParams = new PurchaseParams(
            nodes: 16,
            tolerance: 8,
            uploadFilesize: 8.MB()
        );

        protected override int NumberOfHosts => 5;
        protected override int NumberOfClients => 3;
        protected override ByteSize HostAvailabilitySize => purchaseParams.SlotSize.Multiply(20.0);
        protected override TimeSpan HostAvailabilityMaxDuration => TimeSpan.FromDays(5.0);

        #endregion

        [Test]
        public void SixteenSlots()
        {
            Log("Clients:");
            Log($"{NumberOfClients} purchases with {purchaseParams.Nodes} slots each have slotSize {purchaseParams.SlotSize}");
            var totalSlots = NumberOfClients * purchaseParams.Nodes;
            var totalSlotSize = purchaseParams.SlotSize.Multiply(totalSlots);
            Log($"So there are {totalSlots} total slots for a total size of {totalSlotSize}");

            Log("Hosts:");
            Log($"{NumberOfHosts} hosts have {HostAvailabilitySize} availability each.");
            var maxSlotsPerHost = HostAvailabilitySize.DivUp(purchaseParams.SlotSize);
            var totalSlotCapacity = maxSlotsPerHost * NumberOfHosts;
            Log($"So, each host could fill {maxSlotsPerHost} slots and all hosts could fill {totalSlotCapacity} slots.");

            Assert.That(totalSlotCapacity, Is.GreaterThan(totalSlots));

            var (hosts, clients) = JumpStartHostsAndClients();
            var purchases = clients.Select(CreatePurchase).ToArray();

            foreach (var p in purchases)
            {
                p.WaitForStorageContractStarted();
            }
        }

        private IStoragePurchaseContract CreatePurchase(IArchivistNode client)
        {
            return client.Marketplace.RequestStorage(new StoragePurchaseRequest(
                client.UploadFile(GenerateTestFile(purchaseParams.UploadFilesize)))
            {
                MinRequiredNumberOfNodes = (uint)purchaseParams.Nodes,
                NodeFailureTolerance = (uint)purchaseParams.Tolerance,
            });
        }
    }
}
