using ArchivistClient;
using ArchivistTests;
using NUnit.Framework;
using Utils;

namespace ArchivistReleaseTests.DataTests
{
    [TestFixture]
    public class OneClientTest : ArchivistDistTest
    {
        [Test]
        public void OneClient()
        {
            var node = StartArchivist();

            PerformOneClientTest(node);

            LogNodeStatus(node);
        }

        private void PerformOneClientTest(IArchivistNode primary)
        {
            var testFile = GenerateTestFile(1.MB());

            var contentType = "video/mp4";
            var filename = "testFilename";
            var contentId = primary.UploadFile(testFile, contentType, filename);

            AssertNodesContainFile(contentId, primary);

            var manifest = primary.DownloadManifestOnly(contentId).Manifest;
            Assert.That(manifest.Filename, Is.EqualTo(filename));
            Assert.That(manifest.Mimetype, Is.EqualTo(contentType));

            var downloadedFile = primary.DownloadContent(contentId);

            testFile.AssertIsEqual(downloadedFile);
        }
    }
}
