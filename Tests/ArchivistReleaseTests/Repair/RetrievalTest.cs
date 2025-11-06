using ArchivistClient;
using ArchivistReleaseTests.Utils;
using NUnit.Framework;
using Utils;

namespace ArchivistReleaseTests.Repair
{
    [TestFixture(0, 1)]
    [TestFixture(0, 2)]
    [TestFixture(0, 3)]
    [TestFixture(1, 2)]
    [TestFixture(1, 3)]
    [TestFixture(2, 3)]
    public class RetrievalTest : MarketplaceAutoBootstrapDistTest
    {
        private readonly ulong stopSlotIndex1;
        private readonly ulong stopSlotIndex2;

        public RetrievalTest(int stopSlotIndex1, int stopSlotIndex2)
        {
            this.stopSlotIndex1 = Convert.ToUInt64(stopSlotIndex1);
            this.stopSlotIndex2 = Convert.ToUInt64(stopSlotIndex2);
        }

        #region Setup

        private readonly PurchaseParams purchaseParams = new PurchaseParams(
            nodes: 4,
            tolerance: 2,
            uploadFilesize: 32.MB()
        );


        public RetrievalTest()
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
        public void RetrievabilityTest([Rerun] int rerun)
        {
            if (stopSlotIndex1 == stopSlotIndex2) throw new Exception();

            var (hosts, clients) = JumpStartHostsAndClients();
            var client = clients.Single();

            // We do not start a validator:
            // Validators would cause slots to freed and repair to be activated. We don't want that.
            // In this test, we explicitly want to check the original data is retrievable when the
            // minimum tolerable number of hosts are still alive.

            var contract = CreateStorageRequest(client);
            contract.WaitForStorageContractStarted();
            var contractCid = contract.ContentId;
            client.Stop(waitTillStopped: true);

            var fills = GetOnChainSlotFills(hosts).ToList();
            var fill1 = fills.Single(f => f.SlotFilledEvent.SlotIndex == stopSlotIndex1);
            var fill2 = fills.Single(f => f.SlotFilledEvent.SlotIndex == stopSlotIndex2);

            Log("Stopping 2 hosts that filled a slot.");
            fill1.Host.Stop(waitTillStopped: true);
            fill2.Host.Stop(waitTillStopped: true);

            AssertContentIsRetrievableByNewNode(contractCid);
        }

        private void AssertContentIsRetrievableByNewNode(ContentId cid, bool isRetry = false)
        {
            var checker = StartArchivist(s => s.WithName("checker"));
            try
            {
                var file = checker.DownloadContent(cid);
                if (file == null) throw new Exception("Failed to download content");
                Assert.That(file.GetFilesize(), Is.EqualTo(purchaseParams.UploadFilesize));
            }
            catch (Exception ex)
            {
                Log($"Download failed with: {ex}");
                if (!isRetry)
                {
                    checker.Stop(waitTillStopped: false);
                    Log("Retrying once...");
                    AssertContentIsRetrievableByNewNode(cid, true);
                }
                else
                {
                    throw;
                }
            }
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
