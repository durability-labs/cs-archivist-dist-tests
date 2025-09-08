using ArchivistTests.Helpers;
using Logging;

namespace ArchivistNetDeployer
{
    public class PeerConnectivityChecker
    {
        public void CheckConnectivity(List<ArchivistNodeStartResult> startResults)
        {
            var log = new ConsoleLog();
            var checker = new PeerConnectionTestHelpers(log);
            var nodes = startResults.Select(r => r.ArchivistNode);

            checker.AssertFullyConnected(nodes);
        }
    }
}
