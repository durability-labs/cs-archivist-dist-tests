using ArchivistClient;
using ArchivistPlugin;
using ArchivistReleaseTests.Utils;
using NUnit.Framework;
using Utils;

namespace ArchivistReleaseTests.MarketTests
{
    [TestFixture]
    public class AvailabilitySaturationTest : MarketplaceAutoBootstrapDistTest
    {
        protected override int NumberOfHosts => purchaseParams.Nodes;
        protected override int NumberOfClients => 5;
        protected override bool MonitorProofPeriods => false;

        // The host availablility is only slightly larger than the encoded dataset.
        // And there are 5 datasets but only 4 hosts.
        // Therefore, not everything can fit.
        protected override ByteSize HostAvailabilitySize
            => purchaseParams.EncodedDatasetSize.Multiply(1.1);

        private readonly PurchaseParams purchaseParams = new PurchaseParams(
            nodes: 4,
            tolerance: 2,
            uploadFilesize: 20.MB()
        );

        [Test]
        [Combinatorial]
        public void AvailabilityTest(
            [Rerun] int rerun
        )
        {
            // The host quota is only slightly larger than their availability.
            // We won't interact with the node and use any of the out-of-availability-quota,
            // but, if there's an issue with releasing bytes back to the availability,
            // have a tight-fit quota makes it more likely to reveal this.
            var hosts = StartHosts(s => s.WithStorageQuota(HostAvailabilitySize.Multiply(1.1)));

            StartValidator();
            var clients = StartClients();

            // We want to create many concurrent purchase requests
            // to flood the host worker queues.
            // The requests are more than enough to fill
            // the host availablilties. So, we don't mind
            // if some of them become cancelled.
            // The idea is that concurrency issues in the availability
            // managing system causes the accounting of used space
            // to drift from the real used space, eventually
            // saturating the node with unused but locked up space.

            for (int i = 0; i < 5; i++)
            {
                Cycle(i, hosts, clients);
            }
        }

        private void Cycle(int i, IArchivistNodeGroup hosts, IArchivistNodeGroup clients)
        {
            Log("Cycle " + i);
            Log("Uploading files...");
            var pairs = clients.Select(c =>
            {
                var cid = c.UploadFile(GenerateTestFile(purchaseParams.UploadFilesize));
                return (c, cid);
            }
            ).ToArray();

            // Start all purchases as close to simultaneous as possible:
            Log("Creating purchases...");
            var tasks = pairs.Select(pair => Task.Run(() =>
            {
                return pair.c.Marketplace.RequestStorage(new StoragePurchaseRequest(pair.cid));
            })).ToArray();

            Task.WaitAll(tasks);
            var requests = tasks.Select(t => t.Result).ToArray();

            Log("Waiting for all purchases to finish or cancel...");
            Time.WaitUntil(() =>
            {
                return requests.All(r =>
                {
                    var state = r.GetStatus();
                    return state != null &&
                        (state.IsCancelled || state.IsFinished);
                });
            },
            timeout: TimeSpan.FromMinutes(30.0),
            retryDelay: TimeSpan.FromMinutes(1.0),
            msg: "All purchases finished or cancelled.");

            Log("All purchases are finished or cancelled. All hosts should be empty...");
            AssertHostAvailabilitiesAreEmpty(hosts);
        }
    }
}
