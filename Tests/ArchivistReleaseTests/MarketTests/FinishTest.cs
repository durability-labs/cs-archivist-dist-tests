using ArchivistClient;
using ArchivistPlugin;
using ArchivistReleaseTests.Utils;
using NUnit.Framework;
using Utils;

namespace ArchivistReleaseTests.MarketTests
{
    [TestFixture(5, 3, 1)]
    //[TestFixture(10, 8, 4)]
    public class FinishTest : MarketplaceAutoBootstrapDistTest
    {
        public FinishTest(int hosts, int slots, int tolerance)
        {
            this.hosts = hosts;
            purchaseParams = new PurchaseParams(slots, tolerance, uploadFilesize: 3.MB());
        }

        private readonly TestToken pricePerBytePerSecond = 10.TstWei();
        private readonly int hosts;
        private readonly PurchaseParams purchaseParams;

        protected override int NumberOfHosts => hosts;
        protected override int NumberOfClients => 1;
        protected override ByteSize HostAvailabilitySize => purchaseParams.SlotSize.Multiply(5.1);

        [Test]
        [Combinatorial]
        public void Finish(
            [Rerun] int rerun
        )
        {
            var hosts = StartHosts();
            var client = StartClients().Single();
            StartValidator();
            AssertHostAvailabilitiesAreEmpty(hosts);

            var request = CreateStorageRequest(client);

            request.WaitForStorageContractSubmitted();
            AssertContractIsOnChain(request);

            WaitForContractStarted(request);
            AssertContractSlotsAreFilledByHosts(request, hosts);

            PrintAvailabilities(hosts);

            request.WaitForStorageContractFinished();

            AssertClientHasPaidForContract(pricePerBytePerSecond, client, request, hosts);
            AssertHostsWerePaidForContract(pricePerBytePerSecond, request, hosts);
            AssertHostsCollateralsAreUnchanged(hosts);
            AssertHostAvailabilitiesAreEmpty(hosts);

            PrintAvailabilities(hosts);
        }

        private void PrintAvailabilities(IArchivistNodeGroup hosts)
        {
            foreach (var host in hosts)
            {
                Log("Availabilities for " + host.GetName());
                PrintAvailabilities(host);
            }
        }

        private void PrintAvailabilities(IArchivistNode host)
        {
            var availabilities = host.Marketplace.GetAvailabilities();
            foreach (var a in availabilities)
            {
                PrintAvailability(host, a);
            }
        }

        private void PrintAvailability(IArchivistNode host, StorageAvailability a)
        {
            var reservations = host.Marketplace.GetReservations(a.Id);
            // that logs it.
        }

        private IStoragePurchaseContract CreateStorageRequest(IArchivistNode client)
        {
            var cid = client.UploadFile(GenerateTestFile(purchaseParams.UploadFilesize));
            return client.Marketplace.RequestStorage(new StoragePurchaseRequest(cid)
            {
                MinRequiredNumberOfNodes = (uint)purchaseParams.Nodes,
                NodeFailureTolerance = (uint)purchaseParams.Tolerance,
                PricePerBytePerSecond = pricePerBytePerSecond,
            });
        }
    }
}
