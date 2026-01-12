using ArchivistClient;
using ArchivistPlugin;
using ArchivistReleaseTests.Utils;
using NUnit.Framework;
using Utils;

namespace ArchivistReleaseTests.MarketTests
{
    [TestFixture(5, 10, 5, 10)]
    public class SequentialContracts : MarketplaceAutoBootstrapDistTest
    {
        public SequentialContracts(int hosts, int slots, int tolerance, int sizeMb)
        {
            this.hosts = hosts;
            purchaseParams = DefaultPurchase
                .WithUploadFilesize(sizeMb.MB())
                .WithNodes(slots)
                .WithTolerance(tolerance);
        }

        private readonly int hosts;
        private readonly PurchaseParams purchaseParams;

        protected override int NumberOfHosts => hosts;
        protected override int NumberOfClients => 4;
        protected override TimeSpan HostAvailabilityMaxDuration => GetContractDuration() * 2;

        [Test]
        [Combinatorial]
        public void Sequential(
            [Values(5)] int numGenerations)
        {
            var (hosts, clients, validator) = JumpStart();

            for (var i = 0; i < numGenerations; i++)
            {
                Log("Generation: " + i);
                try
                {
                    Generation(clients, hosts);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Failed at generation {i} with exception {ex}");
                }
            }

            Thread.Sleep(TimeSpan.FromSeconds(12.0));
        }

        private void Generation(IArchivistNodeGroup clients, IArchivistNodeGroup hosts)
        {
            var requests = All(clients.ToArray(), CreateStorageRequest);

            All(requests, r =>
            {
                r.WaitForStorageContractSubmitted();
                AssertContractIsOnChain(r);
            });

            All(requests, WaitForContractStarted);
        }

        private void All<T>(T[] items, Action<T> action)
        {
            var tasks = items.Select(r => Task.Run(() => action(r))).ToArray();
            Task.WaitAll(tasks);
            foreach(var t in tasks)
            {
                if (t.Exception != null) throw t.Exception;
            }
        }

        private TResult[] All<T, TResult>(T[] items, Func<T, TResult> action)
        {
            var tasks = items.Select(r => Task.Run(() => action(r))).ToArray();
            Task.WaitAll(tasks);
            foreach (var t in tasks)
            {
                if (t.Exception != null) throw t.Exception;
            }
            return tasks.Select(t => t.Result).ToArray();
        }

        private IStoragePurchaseContract CreateStorageRequest(IArchivistNode client)
        {
            var cid = client.UploadFile(GenerateTestFile(purchaseParams.UploadFilesize));
            var config = GetContracts().Deployment.Config;
            return client.Marketplace.RequestStorage(new StoragePurchaseRequest(cid)
            {
                Duration = GetContractDuration(),
                Expiry = GetContractExpiry(),
                MinRequiredNumberOfNodes = purchaseParams.Nodes,
                NodeFailureTolerance = purchaseParams.Tolerance,
                PricePerBytePerSecond = purchaseParams.PricePerByteSecond,
                ProofProbability = 100000,
                CollateralPerByte = 1.TstWei()
            });
        }

        private TimeSpan GetContractExpiry()
        {
            return GetContractDuration() / 2;
        }

        private TimeSpan GetContractDuration()
        {
            return Get8TimesConfiguredPeriodDuration() * 4;
        }

        private TimeSpan Get8TimesConfiguredPeriodDuration()
        {
            var config = GetContracts().Deployment.Config;
            return TimeSpan.FromSeconds(config.Proofs.Period * 8.0);
        }
    }
}
