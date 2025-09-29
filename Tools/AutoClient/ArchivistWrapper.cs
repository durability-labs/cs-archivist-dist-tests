using ArchivistClient;
using FileUtils;
using Utils;

namespace AutoClient
{
    public class ArchivistWrapper
    {
        private readonly App app;
        private static readonly Random r = new Random();

        public ArchivistWrapper(App app, IArchivistNode node)
        {
            this.app = app;
            Node = node;
        }

        public IArchivistNode Node { get; }

        public ContentId UploadFile(string filepath)
        {
            return Node.UploadFile(TrackedFile.FromPath(app.Log, filepath));
        }

        public StoragePurchase? GetStoragePurchase(string pid)
        {
            return Node.GetPurchaseStatus(pid);
        }

        public IStoragePurchaseContract RequestStorage(ContentId cid)
        {
            var durability = GetDurabilityValues();
            var result = Node.Marketplace.RequestStorage(new StoragePurchaseRequest(cid)
            {
                CollateralPerByte = app.Config.CollateralPerByte.TstWei(),
                Duration = GetDuration(),
                Expiry = TimeSpan.FromMinutes(app.Config.ContractExpiryMinutes),
                MinRequiredNumberOfNodes = Convert.ToUInt32(durability.Nodes),
                NodeFailureTolerance = Convert.ToUInt32(durability.Tolerance),
                PricePerBytePerSecond = GetPricePerBytePerSecond(),
                ProofProbability = durability.ProofProbability
            });
            return result;
        }

        private DurabilityValues GetDurabilityValues()
        {
            return RandomUtils.GetOneRandom(app.Config.DurabilityValues);
        }

        private TestToken GetPricePerBytePerSecond()
        {
            var i = app.Config.PricePerBytePerSecond;
            i -= 100;
            i += r.Next(0, 1000);

            return i.TstWei();
        }

        private TimeSpan GetDuration()
        {
            var durations = app.Config.Durations;
            if (durations.Length == 1)
            {
                return durations[0];
            }
            if (durations.Length == 2)
            {
                var seconds = r.Next(Convert.ToInt32(durations[0].TotalSeconds), Convert.ToInt32(durations[1].TotalSeconds));
                return TimeSpan.FromSeconds(seconds);
            }
            throw new Exception("Misconfigured DurationMinutes");
        }
    }
}
