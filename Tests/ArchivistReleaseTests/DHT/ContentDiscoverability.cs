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
            [DhtFailRate] int failureRate)
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

    public class DhtFailRateAttribute : ValuesAttribute
    {
        private const int Start = 100;

        public DhtFailRateAttribute()
        {
            var list = new List<object>();
            var value = Start;
            while (value > 1)
            {
                list.Add(value);
                value = value / 2;
            }
            data = list.ToArray();
        }
    }
}
