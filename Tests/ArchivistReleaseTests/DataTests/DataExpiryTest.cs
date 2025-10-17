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
        public void DeletesExpiredDataUsedByStorageRequests()
        {
            var fileSize = 3.MB();

            var bootstrapNode = StartArchivist();
            var geth = StartGethNode(s => s.IsMiner());
            var contracts = Ci.StartArchivistContracts(geth, bootstrapNode.Version);
            var node = StartArchivist(s => WithFastBlockExpiry(s)
                .EnableMarketplace(geth, contracts, m => m.WithInitial(100.Eth(), 100.Tst()))
            );

            var startSpace = node.Space();
            Assert.That(startSpace.QuotaUsedBytes, Is.EqualTo(0));

            var cid = node.UploadFile(GenerateTestFile(fileSize));
            var purchase = node.Marketplace.RequestStorage(new StoragePurchaseRequest(cid));
            var usedSpace = node.Space();
            var usedFiles = node.LocalFiles();
            Assert.That(usedSpace.QuotaUsedBytes, Is.GreaterThanOrEqualTo(fileSize.SizeInBytes));
            Assert.That(usedSpace.FreeBytes, Is.LessThanOrEqualTo(startSpace.FreeBytes - fileSize.SizeInBytes));
            Assert.That(usedFiles.Content.Length, Is.EqualTo(2));

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
        public void StorageRequestsKeepManifests()
        {
            var bootstrapNode = StartArchivist(s => s.WithName("Bootstrap"));
            var geth = StartGethNode(s => s.IsMiner());
            var contracts = Ci.StartArchivistContracts(geth, bootstrapNode.Version);
            var client = StartArchivist(s => WithFastBlockExpiry(s)
                .WithName("client")
                .WithBootstrapNode(bootstrapNode)
                .EnableMarketplace(geth, contracts, m => m.WithInitial(100.Eth(), 100.Tst()))
            );

            var hosts = StartArchivist(3, s => WithFastBlockExpiry(s)
                .WithName("host")
                .WithBootstrapNode(bootstrapNode)
                .EnableMarketplace(geth, contracts, m => m.AsStorageNode().WithInitial(100.Eth(), 100.Tst()))
            );
            foreach (var host in hosts) host.Marketplace.MakeStorageAvailable(new ArchivistClient.CreateStorageAvailability(
                totalSpace: 2.GB(),
                maxDuration: TimeSpan.FromDays(2.0),
                minPricePerBytePerSecond: 1.TstWei(),
                totalCollateral: 10.Tst()));

            var uploadCid = client.UploadFile(GenerateTestFile(5.MB()));
            var request = client.Marketplace.RequestStorage(new ArchivistClient.StoragePurchaseRequest(uploadCid)
            {
                CollateralPerByte = 1.TstWei(),
                Duration = TimeSpan.FromDays(1.0),
                Expiry = TimeSpan.FromHours(1.0),
                MinRequiredNumberOfNodes = 3,
                NodeFailureTolerance = 1,
                PricePerBytePerSecond = 10.TstWei(),
                ProofProbability = 99999
            });
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
