using ArchivistClient;
using ArchivistPlugin;
using ArchivistTests;
using NUnit.Framework;

namespace ArchivistReleaseTests.DHT
{
    [TestFixture(10)]
    public class DhtStabilityTest : AutoBootstrapDistTest
    {
        private readonly int numberOfNodes;

        public DhtStabilityTest(int numberOfNodes)
        {
            this.numberOfNodes = numberOfNodes;
        }

        [Test]
        public void RoutingTableStability()
        {
            var duration = TimeSpan.FromHours(24.0);

            var nodes = StartArchivist(numberOfNodes);

            WaitAndCheck(nameof(RoutingTableStability),
                duration,
                loopTime: TimeSpan.FromMinutes(10),
                check: () => CheckRoutingTables(nodes));
        }

        private void CheckRoutingTables(IArchivistNodeGroup nodes)
        {
            // Each node should have numberOfNodes entries in its routing table.
            // Each entry should have seen = true, but we will allow 
            // a low number of seen = false each cycle.

            var report = new DhtReport();
            foreach (var n in nodes)
            {
                var info = n.GetDebugInfo();
                CheckRoutingTable(n, info.Table.Nodes, report);
            }

            report.Publish(Log);
        }

        private void CheckRoutingTable(IArchivistNode n, DebugInfoTableNode[] nodes, DhtReport report)
        {
            var seen = nodes.Count(n => n.Seen);
            var minSeen = numberOfNodes / 2;

            if (seen >= minSeen) report.AddOK(n.GetName(), seen);
            else report.AddFail(n.GetName(), seen);
        }

        public class DhtReport
        {
            private readonly List<string> ok = new List<string>();
            private readonly List<string> failed = new List<string>();

            public void AddOK(string name, int seen)
            {
                ok.Add($"{name}={seen}");
            }

            public void AddFail(string name, int seen)
            {
                failed.Add($"{name}={seen}");
            }

            public void Publish(Action<string> log)
            {
                log($"[OK] {string.Join(", ", ok)}");
                if (failed.Any())
                {
                    log($"[FAILED] {string.Join(", ", failed)}");
                    Assert.Fail("One or more node routing tables have degraded.");
                }
            }
        }
    }
}
