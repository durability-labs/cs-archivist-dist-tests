using ArchivistTests;
using MetricsPlugin;
using NUnit.Framework;
using Utils;

namespace ArchivistReleaseTests.NodeTests
{
    [TestFixture]
    public class MetricsTest : AutoBootstrapDistTest
    {
        [Test]
        public void BasicMetrics()
        {
            var nodes = StartArchivist(2, s => s.EnableMetrics());
            
            var metrics = Ci.GetMetricsFor(scrapeInterval: TimeSpan.FromSeconds(10), nodes);

            nodes[0].DownloadContent(nodes[1].UploadFile(GenerateTestFile(1.MB())));
            
            metrics[0].AssertThat("libp2p_peers", Is.EqualTo(1));
            metrics[1].AssertThat("libp2p_peers", Is.EqualTo(1));

            metrics[0].AssertThat("archivist_block_exchange_want_block_lists_sent", Is.GreaterThan(15));
            metrics[1].AssertThat("archivist_block_exchange_want_block_lists_received", Is.GreaterThan(15));

            metrics[0].AssertThat("dht_message_requests_incoming", Is.GreaterThan(1));
            metrics[0].AssertThat("dht_message_requests_outgoing", Is.GreaterThan(1));
            metrics[1].AssertThat("dht_message_requests_incoming", Is.GreaterThan(1));
            metrics[1].AssertThat("dht_message_requests_outgoing", Is.GreaterThan(1));
        }
    }
}
