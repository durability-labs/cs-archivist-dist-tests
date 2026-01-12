using ArchivistTests;
using NUnit.Framework;
using Utils;

namespace ArchivistReleaseTests.DHT
{
    [TestFixture]
    public class ContentDiscoverability : AutoBootstrapDistTest
    {
        [Test]
        [Combinatorial]
        public void FullDiscoverabilityWithFailureRate(
            [Values(2, 3)] int failureRate)
        {
            var nodes = StartArchivist(10);

            Log($"Set failure probability: {failureRate}");
            foreach (var n in nodes) n.SetDHTFailureProbability(failureRate);

            // Gives network situation time to affect the DHT records.
            Thread.Sleep(TimeSpan.FromMinutes(6));

            var helper = CreatePeerDownloadTestHelpers(downloadTimeout: TimeSpan.FromSeconds(10));
            helper.AssertFullDownloadInterconnectivity(nodes, 1.MB());
        }
    }
}
