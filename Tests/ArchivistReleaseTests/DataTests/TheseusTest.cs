using ArchivistClient;
using ArchivistPlugin;
using ArchivistTests;
using FileUtils;
using NUnit.Framework;
using Utils;

namespace ArchivistReleaseTests.DataTests
{
    [TestFixture]
    public class TheseusTest : AutoBootstrapDistTest
    {
        private readonly List<IArchivistNode> nodes = new List<IArchivistNode>();
        private TrackedFile file = null!;
        private ContentId cid = new ContentId();

        [SetUp]
        public void Setup()
        {
            file = GenerateTestFile(10.MB());
        }

        [Test]
        [Combinatorial]
        public void Theseus(
            [Values(1, 2)] int remainingNodes,
            [Values(5)] int steps)
        {
            Assert.That(remainingNodes, Is.GreaterThan(0));
            Assert.That(steps, Is.GreaterThan(remainingNodes + 1));

            nodes.AddRange(StartArchivist(remainingNodes + 1));
            cid = nodes.First().UploadFile(file);

            AllNodesHaveFile();

            for (var i = 0; i < steps; i++)
            {
                Log($"{nameof(Theseus)} step {i}");
                nodes[0].Stop(waitTillStopped: true);
                nodes.RemoveAt(0);

                nodes.Add(StartArchivist());

                AllNodesHaveFile();
            }
        }

        private void AllNodesHaveFile()
        {
            Log($"{nameof(AllNodesHaveFile)} {nodes.Names()}");
            foreach (var n in nodes) HasFile(n);
        }

        private void HasFile(IArchivistNode n)
        {
            var downloaded = n.DownloadContent(cid);
            file.AssertIsEqual(downloaded);
        }
    }
}
