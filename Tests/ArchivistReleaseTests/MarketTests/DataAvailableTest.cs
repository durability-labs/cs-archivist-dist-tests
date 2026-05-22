using ArchivistReleaseTests.Utils;
using NUnit.Framework;
using Utils;

namespace ArchivistReleaseTests.MarketTests
{
    [TestFixture]
    public class DataAvailableTest : MarketplaceAutoBootstrapDistTest
    {
        protected override int NumberOfHosts => 5;
        protected override int NumberOfClients => 1;

        private readonly TimeSpan contractDuration = TimeSpan.FromHours(1.0);
        private readonly TimeSpan checkInterval = TimeSpan.FromMinutes(5);
        protected override TimeSpan HostAvailabilityMaxDuration => contractDuration * 2;

        [Test]
        public void StoredDataRemainsAvailable()
        {
            var (hosts, clients, validator) = JumpStart();
            var client = clients.Single();

            var file = GenerateTestFile(400.MB());
            var cid = client.UploadFile(file);
            var request = client.Marketplace.RequestStorage(new ArchivistClient.StoragePurchaseRequest(cid)
            {
                Duration = contractDuration,
            });

            request.WaitForStorageContractStarted();
            var storedCid = request.EncodedContentId;
            var finishUtc = request.GetExpectedFinishUtc() - TimeSpan.FromMinutes(1.0);
            client.Stop(waitTillStopped: true);

            while (DateTime.UtcNow < finishUtc)
            {
                Thread.Sleep(checkInterval);

                AssertDataIsAvailable(file, storedCid);
            }
        }
    }
}
