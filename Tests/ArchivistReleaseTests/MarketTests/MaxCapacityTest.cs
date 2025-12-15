using ArchivistClient;
using ArchivistReleaseTests.Utils;
using NUnit.Framework;
using Utils;

namespace ArchivistReleaseTests.MarketTests
{
    public class MaxCapacityTest : MarketplaceAutoBootstrapDistTest
    {
        private readonly TestToken pricePerBytePerSecond = 10.TstWei();
        private readonly PurchaseParams purchaseParams = new PurchaseParams(
            nodes: 10,
            tolerance: 5,
            uploadFilesize: 10.MB()
        );

        protected override int NumberOfHosts => purchaseParams.Nodes / 2;
        protected override int NumberOfClients => 1;
        protected override ByteSize HostAvailabilitySize => purchaseParams.SlotSize.Multiply(2.1);

        [Test]
        [Combinatorial]
        public void TwoSlotsEach(
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
        }

        private IStoragePurchaseContract CreateStorageRequest(IArchivistNode client)
        {
            var cid = client.UploadFile(GenerateTestFile(purchaseParams.UploadFilesize));
            var config = GetContracts().Deployment.Config;
            return client.Marketplace.RequestStorage(new StoragePurchaseRequest(cid)
            {
                MinRequiredNumberOfNodes = (uint)purchaseParams.Nodes,
                NodeFailureTolerance = (uint)purchaseParams.Tolerance,
                PricePerBytePerSecond = pricePerBytePerSecond,
            });
        }
    }
}
