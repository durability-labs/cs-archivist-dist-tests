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

        [Test]
        public void UploadQuotaLimit()
        {
            var node = StartArchivist(n => n.WithStorageQuota(10.MB()));

            for (var i = 0; i < 9; i++)
            {
                node.UploadFile(GenerateTestFile(1.MB()));
                node.Space();
            }

            try
            {
                node.UploadFile(GenerateTestFile(1.MB()));
                node.Space();
                Assert.Fail("Successfully uploaded a dataset causing the node to exceed its quota.");
            }
            catch (Exception ex)
            {
                Assert.That(ex.Message.Contains("413"),
                    $"Expected HTTP 413 error, but got: {ex.Message}");
                Log("Upload failed with correct HTTP 413 status code.");
            }
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
