using ArchivistClient;
using ArchivistTests;
using NUnit.Framework;
using Utils;

namespace ArchivistReleaseTests.DHT
{
    [TestFixture]
    public class DhtRecoverability : AutoBootstrapDistTest
    {
        private readonly TimeSpan DhtUpdateTimeout = TimeSpan.FromMinutes(15);
        private readonly int numNodes = 20;

        [Test]
        public void AfterFullDisconnect()
        {
            var nodes = StartArchivist(numNodes).ToArray();

            AssertRoutingTablesOk(nodes);

            SetFullDisconnect(nodes);

            AssertRoutingTablesClear(nodes);

            RestoreConnectivitiy(nodes);

            AssertRoutingTablesOk(nodes);
        }

        private void AssertRoutingTablesOk(IArchivistNode[] nodes)
        {
            Log(nameof(AssertRoutingTablesOk));
            WaitUntil(() => nodes.All(RoutingTableOk), nameof(AssertRoutingTablesOk));
        }

        private void AssertRoutingTablesClear(IArchivistNode[] nodes)
        {
            Log(nameof(AssertRoutingTablesClear));
            WaitUntil(() => nodes.All(RoutingTableClear), nameof(AssertRoutingTablesClear));
        }

        private bool RoutingTableOk(IArchivistNode n)
        {
            // We expect node n to know the bootstrap node, plus at least half of its peers.
            var info = n.GetDebugInfo();
            var seenNodes = info.Table.Nodes
                .Where(e => e.Seen)
                .ToArray();

            Log($"{n.GetName()}=[{string.Join(" | ", info.Table.Nodes.Select(s => $"{s.NodeId}({s.Seen})").ToArray())}]");

            var bootnode = seenNodes.SingleOrDefault(e => e.PeerId == BootstrapNode.GetPeerId());

            return
                bootnode != null &&
                seenNodes.Length >= (numNodes / 2);
        }

        private bool RoutingTableClear(IArchivistNode n)
        {
            // We expect node n to only know the bootstrap node
            // and that its seen value is false.
            var info = n.GetDebugInfo();
            var nodes = info.Table.Nodes;
            Log($"{n.GetName()}=[{string.Join(" | ", nodes.Select(s => $"{s.NodeId}({s.Seen})").ToArray())}]");

            return
                nodes.Length == 1 &&
                nodes[0].Seen == false &&
                nodes[0].PeerId == BootstrapNode.GetPeerId();
        }

        private void SetFullDisconnect(IArchivistNode[] nodes)
        {
            Log(nameof(SetFullDisconnect));
            foreach (var n in nodes) n.SetDHTFailureProbability(1);
        }

        private void RestoreConnectivitiy(IArchivistNode[] nodes)
        {
            Log(nameof(RestoreConnectivitiy));
            foreach (var n in nodes) n.SetDHTFailureProbability(0);
        }

        private void WaitUntil(Func<bool> predicate, string msg)
        {
            var duration = Time.WaitUntil(predicate,
                timeout: DhtUpdateTimeout,
                retryDelay: TimeSpan.FromSeconds(30),
                msg: msg);

            Log($"{msg} {Time.FormatDuration(duration)}");
        }
    }
}
