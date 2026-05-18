using ArchivistClient;
using ArchivistTests;
using NUnit.Framework;

namespace ArchivistReleaseTests.DataTests
{
    [TestFixture]
    public class UnknownCidTest : ArchivistDistTest
    {
        [Test]
        public void DownloadingUnknownCidDoesNotCauseCrash()
        {
            var node = StartArchivist();

            var unknownCid = new ContentId("zDvZRwzkzHsok3Z8yMoiXE9EDBFwgr8WygB8s4ddcLzzSwwXAxLZ");

            var localFiles = node.LocalFiles().Content;
            Assert.That(localFiles.Select(f => f.Cid), Does.Not.Contain(unknownCid));

            try
            {
                node.DownloadContent(unknownCid, TimeSpan.FromMinutes(2.0));
            }
            catch (Exception ex)
            {
                var expectedMessage = $"Download of '{unknownCid.Id}' timed out";
                if (!ex.Message.StartsWith(expectedMessage)) throw;
            }

            WaitAndCheckNodesStaysAlive(TimeSpan.FromMinutes(2), node);
        }
    }
}
