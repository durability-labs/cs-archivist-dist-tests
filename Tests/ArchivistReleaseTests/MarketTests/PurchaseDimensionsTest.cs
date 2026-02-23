using ArchivistClient;
using ArchivistReleaseTests.Utils;
using NUnit.Framework;
using Utils;

namespace ArchivistReleaseTests.MarketTests
{
    [TestFixture]
    public class PurchaseDimensionsTest : MarketplaceAutoBootstrapDistTest
    {
        protected override int NumberOfHosts => 1;
        protected override int NumberOfClients => 1;

        [Test]
        [Combinatorial]
        [Ignore("This test is to make sure the PurchaseParams type correctly calculates purchase contract dimensions")]
        public void CheckCalculation(
            [Values(5, 10, 20)] int uploadFilesizeMb,
            [Values(4, 5, 6)] int nodes,
            [Values(1, 2)] int tolerance
        )
        {
            var (hosts, clients) = JumpStartHostsAndClients();
            var client = clients.Single();

            var purchase = client.Marketplace.RequestStorage(new StoragePurchaseRequest(
                client.UploadFile(GenerateTestFile(uploadFilesizeMb.MB()))
            )
            {
                MinRequiredNumberOfNodes = nodes,
                NodeFailureTolerance = tolerance
            });

            purchase.WaitForStorageContractSubmitted();

            Time.WaitUntil(() => GetChainMonitor().Requests.Length == 1, "Wait for request");

            var request = GetChainMonitor().Requests.Single();
            var expected = new PurchaseParams(nodes, tolerance, DefaultStoragePurchase.Duration, uploadFilesizeMb.MB(), DefaultStoragePurchase.PricePerBytePerSecond, DefaultStoragePurchase.CollateralPerByte);

            Assert.That(request.Ask.Slots, Is.EqualTo(expected.Nodes));
            Assert.That(request.Ask.MaxSlotLoss, Is.EqualTo(expected.Tolerance));
            Assert.That(request.Ask.CollateralPerByte, Is.EqualTo(expected.CollateralPerByte));
            Assert.That(request.Ask.PricePerBytePerSecond, Is.EqualTo(expected.PricePerByteSecond));

            Assert.That(request.Ask.Duration, Is.EqualTo(expected.Duration));
            Assert.That(request.Expiry, Is.EqualTo(DefaultStoragePurchase.Expiry));

            client.Space();
            client.LocalFiles();

            Log($"upload filesize: {uploadFilesizeMb.MB().SizeInBytes}");
            Log($"calculated encoded size: {expected.EncodedDatasetSize.SizeInBytes}");

            Log($"slotSize request: {request.Ask.SlotSize} - calculated: {expected.SlotSize}");
            Assert.That(request.Ask.SlotSize, Is.EqualTo(expected.SlotSize));
        }
    }
}
