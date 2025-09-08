using ArchivistClient;
using ArchivistPlugin;
using ArchivistTests;
using NUnit.Framework;
using Utils;

namespace ArchivistReleaseTests.DataTests
{
    [TestFixture]
    public class TwoClientTests : ArchivistDistTest
    {
        [Test]
        public void TwoClientTest()
        {
            var uploader = StartArchivist(s => s.WithName("Uploader"));
            var downloader = StartArchivist(s => s.WithName("Downloader").WithBootstrapNode(uploader));

            PerformTwoClientTest(uploader, downloader);
        }

        [Test]
        [Ignore("Location selection is currently unavailable.")]
        public void TwoClientsTwoLocationsTest()
        {
            var locations = Ci.GetKnownLocations();
            if (locations.NumberOfLocations < 2)
            {
                Assert.Inconclusive("Two-locations test requires 2 nodes to be available in the cluster.");
                return;
            }

            var uploader = Ci.StartArchivistNode(s => s.WithName("Uploader").At(locations.Get(0)));
            var downloader = Ci.StartArchivistNode(s => s.WithName("Downloader").WithBootstrapNode(uploader).At(locations.Get(1)));

            PerformTwoClientTest(uploader, downloader);
        }

        private void PerformTwoClientTest(IArchivistNode uploader, IArchivistNode downloader)
        {
            PerformTwoClientTest(uploader, downloader, 10.MB());
        }

        private void PerformTwoClientTest(IArchivistNode uploader, IArchivistNode downloader, ByteSize size)
        {
            var testFile = GenerateTestFile(size);

            var contentId = uploader.UploadFile(testFile);
            AssertNodesContainFile(contentId, uploader);

            var downloadedFile = downloader.DownloadContent(contentId);
            AssertNodesContainFile(contentId, uploader, downloader);

            testFile.AssertIsEqual(downloadedFile);
            CheckLogForErrors(uploader, downloader);
        }
    }
}
