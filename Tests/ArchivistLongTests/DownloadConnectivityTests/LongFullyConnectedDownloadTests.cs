using ArchivistTests;
using DistTestCore;
using NUnit.Framework;
using Utils;

namespace ArchivistLongTests.DownloadConnectivityTests
{
    [TestFixture]
    public class LongFullyConnectedDownloadTests : AutoBootstrapDistTest
    {
        [Test]
        [UseLongTimeouts]
        [Combinatorial]
        public void FullyConnectedDownloadTest(
            [Values(10, 15, 20)] int numberOfNodes,
            [Values(10, 100)] int sizeMBs)
        {
            var nodes = StartArchivist(numberOfNodes);

            CreatePeerDownloadTestHelpers(downloadTimeout: TimeSpan.FromSeconds(60))
                .AssertFullDownloadInterconnectivity(nodes, sizeMBs.MB());
        }
    }
}
