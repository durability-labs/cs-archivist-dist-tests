using ArchivistClient;
using ArchivistTests;
using NUnit.Framework;

namespace ArchivistReleaseTests.NodeTests
{
    [TestFixture]
    public class PeerTableTests : AutoBootstrapDistTest
    {
        [Test]
        public void PeerTableCompleteness()
        {
            var nodes = StartArchivist(10);

            AssertAllNodesSeeEachOther(nodes.Concat([BootstrapNode!]));
        }

        private void AssertAllNodesSeeEachOther(IEnumerable<IArchivistNode> nodes)
        {
            var helper = CreatePeerConnectionTestHelpers();
            helper.AssertFullyConnected(nodes);
        }
    }
}
