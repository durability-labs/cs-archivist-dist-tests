using ArchivistClient;
using ArchivistContractsPlugin.ChainMonitor;
using ArchivistReleaseTests.Utils;
using Nethereum.Hex.HexConvertors.Extensions;
using NUnit.Framework;
using Utils;

namespace ArchivistReleaseTests.MarketTests
{
    [TestFixture]
    public class GetRequestTest : MarketplaceAutoBootstrapDistTest
    {
        protected override int NumberOfHosts => 5;
        protected override int NumberOfClients => 1;
        protected override ByteSize HostAvailabilitySize => 1.GB();

        protected override bool MonitorChainState => false;

        private ChainMonitor monitor = null!;
        private IChainStateRequest[] requests = Array.Empty<IChainStateRequest>();

        [Test]
        public void GetRequestFromChain()
        {
            var hosts = StartHosts();
            var client = StartClients().Single();
            StartValidator();

            var request = client.Marketplace.RequestStorage(
                new StoragePurchaseRequest(client.UploadFile(GenerateTestFile(10.MB())))
                {
                    ProofProbability = 1
                }
            );

            request.WaitForStorageContractStarted();

            Log("Starting ChainMonitor...");
            var failed = false;
            monitor = new ChainMonitor(GetTestLog(), GetGeth(), GetContracts(), this, DateTime.UtcNow, TimeSpan.FromSeconds(3.0), true);
            monitor.Start(() =>
            {
                failed = true;
                Log("Failure in chain monitor.");
                Assert.Fail("Failure in chain monitor.");
            });

            // We trigger some failure so that there are events from which we can learn the requestId,
            // whose creation even we missed, so we'll use the view function to fetch it.
            // (proof-submit events don't contain requestIds.)
            var fills = GetOnChainSlotFills(hosts);
            fills.First().Host.Stop(waitTillStopped: false);

            request.WaitForStorageContractFinished();

            Assert.That(failed, Is.False);
            Assert.That(requests.Length, Is.EqualTo(1));
            Assert.That(requests[0].RequestId.ToHex(), Is.EqualTo(request.PurchaseId));
        }

        protected override void OnPeriod(PeriodReport report)
        {
            base.OnPeriod(report);

            if (monitor != null && monitor.Requests.Any())
            {
                requests = monitor.Requests;
            }
        }
    }
}
