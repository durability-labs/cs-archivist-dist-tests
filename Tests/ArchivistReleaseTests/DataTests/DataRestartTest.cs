using ArchivistTests;
using NUnit.Framework;
using Utils;

namespace ArchivistReleaseTests.DataTests
{
    [TestFixture]
    public class DataRestartTest : ArchivistDistTest
    {
        [Test]
        public void KeepsData()
        {
            var node = StartArchivist();

            var file = GenerateTestFile(10.MB());
            var cid = node.UploadFile(file);

            node.InPlaceRestart();

            var download1 = node.DownloadContent(cid);

            node.InPlaceRestart();

            var download2 = node.DownloadContent(cid);

            file.AssertIsEqual(download1);
            file.AssertIsEqual(download2);
        }
    }
}
