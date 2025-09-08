using ArchivistTests;
using NUnit.Framework;
using Utils;

namespace ArchivistReleaseTests.DataTests
{
    [TestFixture]
    public class StreamlessDownloadTest : ArchivistDistTest
    {
        [Test]
        public void StreamlessTest()
        {
            var uploader = StartArchivist();
            var downloader = StartArchivist(s => s.WithBootstrapNode(uploader));

            var size = 10.MB();
            var file = GenerateTestFile(size);
            var cid = uploader.UploadFile(file);

            var startSpace = downloader.Space();
            var start = DateTime.UtcNow;
            var localDataset = downloader.DownloadStreamlessWait(cid, size);

            Assert.That(localDataset.Cid, Is.EqualTo(cid));
            Assert.That(localDataset.Manifest.DatasetSize.SizeInBytes, Is.EqualTo(file.GetFilesize().SizeInBytes));

            // Stop the uploader node and verify that the downloader has the data.
            uploader.Stop(waitTillStopped: true);
            var downloaded = downloader.DownloadContent(cid);
            file.AssertIsEqual(downloaded);
        }
    }
}
