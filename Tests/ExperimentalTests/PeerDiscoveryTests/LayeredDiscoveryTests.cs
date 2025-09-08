using ArchivistClient;
using ArchivistTests;
using NUnit.Framework;

namespace ExperimentalTests.PeerDiscoveryTests
{
    [TestFixture]
    public class LayeredDiscoveryTests : ArchivistDistTest
    {
        [Test]
        public void TwoLayersTest()
        {
            var root = StartArchivist();
            var l1Source = StartArchivist(s => s.WithBootstrapNode(root));
            var l1Node = StartArchivist(s => s.WithBootstrapNode(root));
            var l2Target = StartArchivist(s => s.WithBootstrapNode(l1Node));

            AssertAllNodesConnected(root, l1Source, l1Node, l2Target);
        }

        [Test]
        public void ThreeLayersTest()
        {
            var root = StartArchivist();
            var l1Source = StartArchivist(s => s.WithBootstrapNode(root));
            var l1Node = StartArchivist(s => s.WithBootstrapNode(root));
            var l2Node = StartArchivist(s => s.WithBootstrapNode(l1Node));
            var l3Target = StartArchivist(s => s.WithBootstrapNode(l2Node));

            AssertAllNodesConnected(root, l1Source, l1Node, l2Node, l3Target);
        }

        [TestCase(3)]
        [TestCase(5)]
        [TestCase(10)]
        public void NodeChainTest(int chainLength)
        {
            var nodes = new List<IArchivistNode>();
            var node = StartArchivist();
            nodes.Add(node);

            for (var i = 1; i < chainLength; i++)
            {
                node = StartArchivist(s => s.WithBootstrapNode(node));
                nodes.Add(node);
            }

            AssertAllNodesConnected(nodes.ToArray());
        }

        private void AssertAllNodesConnected(params IArchivistNode[] nodes)
        {
            CreatePeerConnectionTestHelpers().AssertFullyConnected(nodes);
        }
    }
}
