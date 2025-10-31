using ArchivistClient;
using ArchivistReleaseTests.Utils;
using NUnit.Framework;
using Utils;

namespace ArchivistReleaseTests.MarketTests
{
    public class RepairRetrievalTest : MarketplaceAutoBootstrapDistTest
    {
        #region Setup

        private readonly PurchaseParams purchaseParams = new PurchaseParams(
            nodes: 4,
            tolerance: 2,
            uploadFilesize: 32.MB()
        );

        public RepairRetrievalTest()
        {
            Assert.That(purchaseParams.Nodes, Is.LessThan(NumberOfHosts));
        }

        protected override int NumberOfHosts => 5;
        protected override int NumberOfClients => 1;
        protected override ByteSize HostAvailabilitySize => purchaseParams.SlotSize.Multiply(1.1); // Each host can hold 1 slot.
        protected override TimeSpan HostAvailabilityMaxDuration => TimeSpan.FromDays(5.0);
        protected override bool MonitorProofPeriods => false;

        #endregion

        [Test]
        [Combinatorial]
        public void RetrievabilityTest(
            [Values(0, 1, 2, 3)] int stopSlotIndex1,
            [Values(0, 1, 2, 3)] int stopSlotIndex2
        )
        {
            if (stopSlotIndex1 == stopSlotIndex2) return;
            var index1 = Convert.ToUInt64(stopSlotIndex1);
            var index2 = Convert.ToUInt64(stopSlotIndex2);

            var hosts = StartHosts().ToList();
            var client = StartClients().Single();

            var contract = CreateStorageRequest(client);
            contract.WaitForStorageContractStarted();
            var contractCid = contract.ContentId;
            client.Stop(waitTillStopped: true);

            var fills = GetOnChainSlotFills(hosts).ToList();

            var fill1 = fills.Single(f => f.SlotFilledEvent.SlotIndex == index1);
            var fill2 = fills.Single(f => f.SlotFilledEvent.SlotIndex == index2);

            fill1.Host.Stop(waitTillStopped: true);
            fill2.Host.Stop(waitTillStopped: true);

            AssertContentIsRetrievableByNewNode(contractCid);
        }

        private void AssertContentIsRetrievableByNewNode(ContentId cid)
        {
            var checker = StartArchivist(s => s.WithName("checker"));
            var file = checker.DownloadContent(cid);
            if (file == null) throw new Exception("Failed to download content");
            Assert.That(file.GetFilesize(), Is.EqualTo(purchaseParams.UploadFilesize));
        }

        private IStoragePurchaseContract CreateStorageRequest(IArchivistNode client)
        {
            var cid = client.UploadFile(GenerateTestFile(purchaseParams.UploadFilesize));
            return client.Marketplace.RequestStorage(new StoragePurchaseRequest(cid)
            {
                MinRequiredNumberOfNodes = (uint)purchaseParams.Nodes,
                NodeFailureTolerance = (uint)purchaseParams.Tolerance,
            });
        }
    }
}
