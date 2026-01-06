using ArchivistClient;
using ArchivistReleaseTests.Utils;
using NUnit.Framework;

namespace ArchivistReleaseTests.MarketTests
{
    [TestFixture(5, 3, 1)]
    [TestFixture(10, 8, 4)]
    public class FinishTest : MarketplaceAutoBootstrapDistTest
    {
        private readonly int hosts;
        private readonly PurchaseParams purchaseParams;

        public FinishTest(int hosts, int slots, int tolerance)
        {
            this.hosts = hosts;
            purchaseParams = DefaultPurchase
                .WithNodes(slots)
                .WithTolerance(tolerance);
        }

        protected override int NumberOfHosts => hosts;
        protected override int NumberOfClients => 1;

        [Test]
        public void Finish()
        {
            var (hosts, clients, validator) = JumpStart();
            var client = clients.Single();

            AssertHostsAreEmpty(hosts);

            var request = CreateStorageRequest(client);

            request.WaitForStorageContractSubmitted();
            AssertContractIsOnChain(request);

            WaitForContractStarted(request);
            AssertContractSlotsAreFilledByHosts(request, hosts);

            request.WaitForStorageContractFinished();

            AssertClientHasPaidForContract(DefaultPurchase.PricePerByteSecond, client, request, hosts);
            AssertHostsWerePaidForContract(DefaultPurchase.PricePerByteSecond, request, hosts);
            AssertHostsCollateralsAreUnchanged(hosts);
            AssertHostsAreEmpty(hosts);
        }

        private IStoragePurchaseContract CreateStorageRequest(IArchivistNode client)
        {
            var cid = client.UploadFile(GenerateTestFile(purchaseParams.UploadFilesize));
            return client.Marketplace.RequestStorage(new StoragePurchaseRequest(cid)
            {
                MinRequiredNumberOfNodes = purchaseParams.Nodes,
                NodeFailureTolerance = purchaseParams.Tolerance,
                PricePerBytePerSecond = DefaultPurchase.PricePerByteSecond,
            });
        }
    }
}
