using ArchivistClient;
using ArchivistTests;
using FileUtils;
using NUnit.Framework;
using System.Diagnostics;
using Utils;

namespace ArchivistReleaseTests.DataTests
{
    public class InterruptUploadTest : ArchivistDistTest
    {
        [Test]
        public void UploadInterruptTest()
        {
            var nodes = StartArchivist(10);

            var tasks = nodes.Select(n => Task<bool>.Run(() => RunInterruptUploadTest(n)));
            Task.WaitAll(tasks.ToArray());

            Assert.That(tasks.Select(t => t.Result).All(r => r == true));

            WaitAndCheckNodesStaysAlive(TimeSpan.FromMinutes(2), nodes);
        }

        private bool RunInterruptUploadTest(IArchivistNode node)
        {
            var file = GenerateTestFile(300.MB());

            var process = StartCurlUploadProcess(node, file);

            Thread.Sleep(500);
            process.Kill();
            Thread.Sleep(1000);

            var log = node.DownloadLog();
            return !log.GetLinesContaining("Unhandled exception in async proc, aborting").Any();
        }

        private Process StartCurlUploadProcess(IArchivistNode node, TrackedFile file)
        {
            var apiAddress = node.GetApiEndpoint();
            var archivistUrl = $"{apiAddress}/api/archivist/v1/data";
            var filePath = file.Filename;
            return Process.Start("curl", $"-X POST {archivistUrl} -H \"Content-Type: application/octet-stream\" -T {filePath}");
        }
    }
}
