using ArchivistClient;
using ArchivistReleaseTests.Utils;
using NUnit.Framework;
using Utils;

namespace ArchivistReleaseTests.MarketTests
{
    [TestFixture]
    public class ExpiryTest : MarketplaceAutoBootstrapDistTest
    {
        protected override int NumberOfHosts => 1;
        protected override int NumberOfClients => 1;
        protected override TestToken HostStartingBalance => DefaultPurchase.CollateralRequiredPerSlot * 1.1; // The host can hold 1 slot.
        protected override TimeSpan HostAvailabilityMaxDuration => TimeSpan.FromDays(3.0);
        // ! host with this collateral filled 3 slots! :(
        protected override TimeSpan HostBlockTTL => TimeSpan.FromDays(1);
        // A large default TTL will reveal if blocks aren't stored locally with the correct contract-expiry value.

        [Test]
        public void PurchaseExpires()
        {
            var (hosts, clients) = JumpStartHostsAndClients();
            var client = clients.Single();

            var request = client.Marketplace.RequestStorage(new StoragePurchaseRequest(
                client.UploadFile(GenerateTestFile(DefaultStoragePurchase.UploadFileSize))
            )
            {
                // A large duration will reveal if blocks are stored with duration-TTL instead of expiry.
                Duration = TimeSpan.FromHours(12.0),
            });

            Time.WaitUntil(() =>
            {
                var fills = GetOnChainSlotFills(hosts);
                return fills.Length > 0;
            },
            timeout: DefaultStoragePurchase.Expiry,
            retryDelay: TimeSpan.FromSeconds(30),
            msg: $"Expected 1 slot fill.");

            request.WaitForStorageContractExpired();

            AssertHostsAreEmpty(hosts, DefaultStoragePurchase.Expiry);
        }
    }
}
