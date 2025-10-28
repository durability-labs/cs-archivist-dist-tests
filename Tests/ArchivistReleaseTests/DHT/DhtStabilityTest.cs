using ArchivistTests;
using NUnit.Framework;
using Utils;

namespace ArchivistReleaseTests.DHT
{
    [TestFixture]
    public class DhtStabilityTest : AutoBootstrapDistTest
    {
        [Test]
        [Combinatorial]
        public void RoutingTableStability(
            [Values(10000, 1000, 100, 10)] int failureRate)
        {
            var nodes = StartArchivist(10);

            Log($"Set DHT failure probability: {failureRate}");
            foreach (var n in nodes) n.SetDHTFailureProbability(failureRate);

            Thread.Sleep(TimeSpan.FromMinutes(12));

            var helper = CreatePeerDownloadTestHelpers();
            helper.AssertFullDownloadInterconnectivity(nodes, 1.MB());
        }
    }
}
