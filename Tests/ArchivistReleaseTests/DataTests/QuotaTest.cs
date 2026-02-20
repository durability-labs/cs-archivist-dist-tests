using ArchivistTests;
using NUnit.Framework;
using Utils;

namespace ArchivistReleaseTests.DataTests
{
    [TestFixture]
    public class QuotaTest : AutoBootstrapDistTest
    {
        [Test]
        public void UploadQuotaCheck()
        {
            var quota = 10.MB();
            var node = StartArchivist(a => a.WithStorageQuota(quota));

            for (var i = 0; i < 9; i++)
            {
                node.UploadFile(GenerateTestFile(1.MB()));
            }

            FailsTo(() =>
            {
                node.UploadFile(GenerateTestFile(1.MB()));
            }, "Upload more data than quota can hold.");
            
            WaitAndCheckNodesStaysAlive(TimeSpan.FromMinutes(1), node);
        }

        [Test]
        public void DownloadQuotaCheck()
        {
            var uploader = StartArchivist();
            var downloader = StartArchivist(s => s.WithStorageQuota(1.MB()));

            var cid = uploader.UploadFile(GenerateTestFile(10.MB()));

            FailsTo(() =>
            {
                downloader.DownloadContent(cid);
            }, "Download more data into a node than quota can hold.");

            WaitAndCheckNodesStaysAlive(TimeSpan.FromMinutes(1), downloader);
        }

        public void FailsTo(Action action, string description)
        {
            var success = false;
            try
            {
                action();
                Assert.Fail($"{description} was supposed to fail, but didn't.");
            }
            catch
            {
                success = true;
                Log($"Successfully failed to {description}.");
            }

            Assert.That(success, Is.True);
        }
    }
}
