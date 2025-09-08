using ArchivistClient;
using ArchivistTests;
using ArchivistTests.Helpers;
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
            var helper = new PeerConnectionTestHelpers(GetTestLog());
            helper.AssertFullyConnected(nodes);
        }
    }
}
