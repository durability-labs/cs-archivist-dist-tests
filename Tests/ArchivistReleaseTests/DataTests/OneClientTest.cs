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

            var contentId = primary.UploadFile(testFile);

            AssertNodesContainFile(contentId, primary);

            var downloadedFile = primary.DownloadContent(contentId);

            testFile.AssertIsEqual(downloadedFile);
        }
    }
}
