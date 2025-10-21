using ArchivistClient;
using ArchivistTests;
using NUnit.Framework;
using Utils;

namespace ArchivistReleaseTests.DHT
{
    [TestFixture]
    public class DhtStabilityTest : AutoBootstrapDistTest
    {
        private int NumberOfNodes => 30;
        private int MinTotal => NumberOfNodes - 5;
        private int MinSeen => NumberOfNodes / 2;
        private int failCount = 0;
        private DateTime failStartUtc = DateTime.MinValue;

        [Test]
        public void RoutingTableStability()
        {
            var duration = TimeSpan.FromHours(24.0);

            var nodes = StartArchivist(NumberOfNodes);

            // We take 30 minutes where we don't count problems.
            // This gives everyone time to discovery everybody.
            failCount = 0;
            failStartUtc = DateTime.UtcNow + TimeSpan.FromMinutes(30.0);

            Log($"minTotal: {MinTotal}");
            Log($"minSeen: {MinSeen}");
            Log($"failStartUtc: {Time.FormatTimestamp(failStartUtc)}");

            WaitAndCheck(nameof(RoutingTableStability),
                duration,
                loopTime: TimeSpan.FromSeconds(30),
                check: () => CheckRoutingTables(nodes));
        }

        private void CheckRoutingTables(IEnumerable<IArchivistNode> nodes)
        {
            // Each node should have numberOfNodes entries in its routing table,
            // but we allow missing a small number.

            // Each entry should have seen = true, but we will allow 
            // a low number of seen = false.

            var report = new DhtReport(
                minTotal: MinTotal,
                minSeen: MinSeen
            );

            foreach (var n in nodes)
            {
                var info = n.GetDebugInfo();
                report.Add(n.GetName(), info.Table.Nodes);
            }

            report.Publish(Log, onFailed: () =>
            {
                if (DateTime.UtcNow < failStartUtc) return;
                failCount++;
                Log($"[DHT check failed] ({failCount})");
                if (failCount > 10) Assert.Fail("One or more node routing tables have degraded.");
            });
        }

        public class DhtReport
        {
            private readonly List<string> tags = new List<string>();
            private readonly int minTotal;
            private readonly int minSeen;
            private bool failed = false;

            public DhtReport(int minTotal, int minSeen)
            {
                this.minTotal = minTotal;
                this.minSeen = minSeen;
            }

            public void Add(string name, DebugInfoTableNode[] nodes)
            {
                var totalOK = nodes.Length >= minTotal;
                var seenOK = nodes.Count(n => n.Seen) >= minSeen;

                tags.Add($"[{name}=({string.Join(",", 
                    nodes
                        .OrderBy(n => n.NodeId)
                        .Select(n => Tag(n)))})]");

                if (!totalOK || !seenOK) failed = true;
            }

            private string Tag(DebugInfoTableNode n)
            {
                if (n.Seen) return $"-{n.NodeId}-";
                return $" {n.NodeId} ";
            }

            public void Publish(Action<string> log, Action onFailed)
            {
                log($"{string.Join(" ", tags)}");
                if (failed)
                {
                    onFailed();
                }
            }
        }
    }
}
