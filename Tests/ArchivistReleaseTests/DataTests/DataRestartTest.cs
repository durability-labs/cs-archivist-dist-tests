using ArchivistTests;
using NUnit.Framework;
using Utils;

namespace ArchivistReleaseTests.DataTests
{
    [TestFixture]
    public class DataRestartTest : AutoBootstrapDistTest
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

        [Test]
        public void SendAfterRestart()
        {
            var uploader = StartArchivist(s => s.WithName("restart_uploader"));
            var downloader = StartArchivist(s => s.WithName("downloader"));

            var file = GenerateTestFile(10.MB());
            var cid = uploader.UploadFile(file);

            uploader.InPlaceRestart();

            var downloaded = downloader.DownloadContent(cid);

            file.AssertIsEqual(downloaded);
        }

        [Test]
        public void ReceiveAfterRestart()
        {
            var uploader = StartArchivist(s => s.WithName("uploader"));
            var downloader = StartArchivist(s => s.WithName("restart_downloader"));

            var file = GenerateTestFile(10.MB());
            var cid = uploader.UploadFile(file);

            downloader.InPlaceRestart();

            var downloaded = downloader.DownloadContent(cid);

            file.AssertIsEqual(downloaded);
        }
    }
}
