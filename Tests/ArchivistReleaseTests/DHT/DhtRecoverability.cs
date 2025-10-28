using ArchivistClient;
using ArchivistTests;
using NUnit.Framework;

namespace ArchivistReleaseTests.DHT
{
    [TestFixture]
    public class DhtRecoverability : AutoBootstrapDistTest
    {
        [Test]
        [Combinatorial]
        public void AfterFullDisconnect(
            [Rerun] int run)
        {
            var nodes = StartArchivist(20).ToArray();

            AssertRoutingTablesOk(nodes);

            SetFullDisconnect(nodes);

            AssertRoutingTablesClear(nodes);

            RestoreConnectivitiy(nodes);

            AssertRoutingTablesOk(nodes);
        }

        private void AssertRoutingTablesOk(IArchivistNode[] nodes)
        {
            Log(nameof(AssertRoutingTablesOk));
            foreach (var n in nodes) AssertRoutingTableOk(n, nodes);
        }

        private void AssertRoutingTableOk(IArchivistNode n, IArchivistNode[] peers)
        {
            // We expect node n to know the bootstrap node, plus at least half of its peers.
            var info = n.GetDebugInfo();
            var seenNodes = info.Table.Nodes
                .Where(e => e.Seen)
                .ToArray();

            Log($"{n.GetName()}=[{string.Join(" | ", info.Table.Nodes.Select(s => $"{s.NodeId}({s.Seen})").ToArray())}]");

            var bootnode = seenNodes.SingleOrDefault(e => e.PeerId == BootstrapNode.GetPeerId());
            Assert.That(bootnode, Is.Not.Null);
            Assert.That(seenNodes.Length, Is.GreaterThanOrEqualTo(peers.Length / 2));
        }

        private void AssertRoutingTablesClear(IArchivistNode[] nodes)
        {
            Log(nameof(AssertRoutingTablesClear));
            foreach (var n in nodes) AssertRoutingTableClear(n);
        }

        private void AssertRoutingTableClear(IArchivistNode n)
        {
            // We expect node n to only know the bootstrap node
            // and that its seen value is false.
            var info = n.GetDebugInfo();
            var nodes = info.Table.Nodes;
            Log($"{n.GetName()}=[{string.Join(" | ", nodes.Select(s => $"{s.NodeId}({s.Seen})").ToArray())}]");

            Assert.That(nodes.Length, Is.EqualTo(1));
            Assert.That(nodes[0].Seen, Is.False);
            Assert.That(nodes[0].PeerId, Is.EqualTo(BootstrapNode.GetPeerId()));
        }

        private void SetFullDisconnect(IArchivistNode[] nodes)
        {
            Log(nameof(SetFullDisconnect));
            foreach (var n in nodes) n.SetDHTFailureProbability(1);

            PoliteDelay();
        }

        private void RestoreConnectivitiy(IArchivistNode[] nodes)
        {
            Log(nameof(RestoreConnectivitiy));
            foreach (var n in nodes) n.SetDHTFailureProbability(0);

            PoliteDelay();
        }

        private void PoliteDelay()
        {
            // It takes a while for the DHT to check its records and/or recover.
            Thread.Sleep(TimeSpan.FromMinutes(6));
        }
    }
}
