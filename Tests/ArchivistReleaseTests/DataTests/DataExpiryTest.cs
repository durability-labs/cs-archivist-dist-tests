using ArchivistClient;
using ArchivistContractsPlugin;
using ArchivistPlugin;
using ArchivistTests;
using NUnit.Framework;
using Utils;

namespace ArchivistReleaseTests.DataTests
{
    [TestFixture]
    public class DataExpiryTest : ArchivistDistTest
    {
        private readonly TimeSpan blockTtl = TimeSpan.FromMinutes(1.0);
        private readonly TimeSpan blockInterval = TimeSpan.FromSeconds(10.0);
        private readonly int blockCount = 100000;

        private IArchivistSetup WithFastBlockExpiry(IArchivistSetup setup)
        {
            return setup
                .WithBlockTTL(blockTtl)
                .WithBlockMaintenanceInterval(blockInterval)
                .WithBlockMaintenanceNumber(blockCount);
        }

        [Test]
        public void DeletesExpiredData()
        {
            var fileSize = 3.MB();
            var node = StartArchivist(s => WithFastBlockExpiry(s));

            var startSpace = node.Space();
            Assert.That(startSpace.QuotaUsedBytes, Is.EqualTo(0));

            node.UploadFile(GenerateTestFile(fileSize));
            var usedSpace = node.Space();
            var usedFiles = node.LocalFiles();
            Assert.That(usedSpace.QuotaUsedBytes, Is.GreaterThanOrEqualTo(fileSize.SizeInBytes));
            Assert.That(usedSpace.FreeBytes, Is.LessThanOrEqualTo(startSpace.FreeBytes - fileSize.SizeInBytes));
            Assert.That(usedFiles.Content.Length, Is.EqualTo(1));

            Thread.Sleep(blockTtl * 2);

            var cleanupSpace = node.Space();
            var cleanupFiles = node.LocalFiles();

            Assert.That(cleanupSpace.QuotaUsedBytes, Is.LessThan(usedSpace.QuotaUsedBytes));
            Assert.That(cleanupSpace.FreeBytes, Is.GreaterThan(usedSpace.FreeBytes));
            Assert.That(cleanupFiles.Content.Length, Is.EqualTo(0));

            Assert.That(cleanupSpace.QuotaUsedBytes, Is.EqualTo(startSpace.QuotaUsedBytes));
            Assert.That(cleanupSpace.FreeBytes, Is.EqualTo(startSpace.FreeBytes));
        }

        [Test]
        public void DeletesExpiredClientDataUsedByStorageRequests()
        {
            var fileSize = 3.MB();

            var bootstrapNode = StartArchivist();
            var geth = StartGethNode(s => s.IsMiner());
            var contracts = Ci.StartArchivistContracts(geth, bootstrapNode.Version);
            var client = StartArchivist(s => WithFastBlockExpiry(s)
                .EnableMarketplace(geth, contracts, m => m.WithInitial(100.Eth(), 100.Tst()))
            );

            var startSpace = client.Space();
            Assert.That(startSpace.QuotaUsedBytes, Is.EqualTo(0));

            var cid = client.UploadFile(GenerateTestFile(fileSize));
            var purchase = client.Marketplace.RequestStorage(new StoragePurchaseRequest(cid));
            var usedSpace = client.Space();
            var usedFiles = client.LocalFiles();
            Assert.That(usedSpace.QuotaUsedBytes, Is.GreaterThanOrEqualTo(fileSize.SizeInBytes));
            Assert.That(usedSpace.FreeBytes, Is.LessThanOrEqualTo(startSpace.FreeBytes - fileSize.SizeInBytes));
            Assert.That(usedFiles.Content.Length, Is.EqualTo(2));

            Thread.Sleep(blockTtl * 2);

            var cleanupSpace = client.Space();
            var cleanupFiles = client.LocalFiles();

            Assert.That(cleanupSpace.QuotaUsedBytes, Is.LessThan(usedSpace.QuotaUsedBytes));
            Assert.That(cleanupSpace.FreeBytes, Is.GreaterThan(usedSpace.FreeBytes));
            Assert.That(cleanupFiles.Content.Length, Is.EqualTo(0));

            Assert.That(cleanupSpace.QuotaUsedBytes, Is.EqualTo(startSpace.QuotaUsedBytes));
            Assert.That(cleanupSpace.FreeBytes, Is.EqualTo(startSpace.FreeBytes));
        }

        [Test]
        public void HostsDoNotDeleteRequestManifests()
        {
            var bootstrapNode = StartArchivist(s => s.WithName("Bootstrap"));
            var geth = StartGethNode(s => s.IsMiner());
            var contracts = Ci.StartArchivistContracts(geth, bootstrapNode.Version);
            var client = StartArchivist(s => WithFastBlockExpiry(s)
                .WithName("client")
                .WithBootstrapNode(bootstrapNode)
                .EnableMarketplace(geth, contracts, m => m.WithInitial(100.Eth(), 100.Tst()))
            );

            var hosts = StartArchivist(4, s => WithFastBlockExpiry(s)
                .WithName("host")
                .WithBootstrapNode(bootstrapNode)
                .EnableMarketplace(geth, contracts, m => m.AsStorageNode().WithInitial(100.Eth(), 100.Tst()))
            );
            foreach (var host in hosts) host.Marketplace.MakeStorageAvailable(new CreateStorageAvailability(
                maxDuration: TimeSpan.FromDays(2.0),
                untilUtc: DateTime.UtcNow + TimeSpan.FromDays(30.0),
                minPricePerBytePerSecond: 1.TstWei(),
                maxCollateralPerByte: 10.Tst()));

            var uploadCid = client.UploadFile(GenerateTestFile(DefaultStoragePurchase.UploadFileSize));
            var request = client.Marketplace.RequestStorage(new StoragePurchaseRequest(uploadCid));
            request.WaitForStorageContractSubmitted();
            request.WaitForStorageContractStarted();
            var storeCid = request.ContentId;

            var clientManifest = client.DownloadManifestOnly(storeCid);
            Assert.That(clientManifest.Manifest.Protected, Is.True);

            client.Stop(waitTillStopped: true);
            Thread.Sleep(blockTtl * 2.0);

            var checker = StartArchivist(s => s.WithName("checker").WithBootstrapNode(bootstrapNode));
            var manifest = checker.DownloadManifestOnly(storeCid);
            Assert.That(manifest.Manifest.Protected, Is.True);
        }
    }
}
