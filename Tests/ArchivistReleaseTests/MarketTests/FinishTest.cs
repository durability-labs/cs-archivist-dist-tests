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
            purchaseParams = new PurchaseParams(
                nodes: slots,
                tolerance: tolerance,
                duration: DefaultPurchase.Duration,
                uploadFilesize: DefaultPurchase.UploadFilesize,
                pricePerByteSecond: DefaultPurchase.PricePerByteSecond,
                collateralPerByte: DefaultPurchase.CollateralPerByte
            );
        }

        protected override int NumberOfHosts => hosts;
        protected override int NumberOfClients => 1;

        [Test]
        [Combinatorial]
        public void Finish(
            [Rerun] int rerun
        )
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
                MinRequiredNumberOfNodes = (uint)purchaseParams.Nodes,
                NodeFailureTolerance = (uint)purchaseParams.Tolerance,
                PricePerBytePerSecond = DefaultPurchase.PricePerByteSecond,
            });
        }
    }
}
