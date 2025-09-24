using Newtonsoft.Json;

namespace BlockchainUtils
{
    public class BlockCache
    {
        public delegate void CacheClearedEvent();

        private const int MaxBuckets = 10;
        private readonly Dictionary<ulong, BlockBucket> buckets = new Dictionary<ulong, BlockBucket>();
        private readonly IBlockBucketStore store;

        public BlockCache()
            : this(new MemoryBlockBucketStore())
        {
        }

        public BlockCache(IBlockBucketStore store)
        {
            this.store = store;
        }

        public BlockTimeEntry Earliest { get; private set; } = null!;
        public BlockTimeEntry Current { get; private set; } = null!;

        public void SetEarliest(BlockTimeEntry entry)
        {
            Earliest = entry;
        }

        public void SetCurrent(BlockTimeEntry entry)
        {
            Current = entry;
        }

        public BlockTimeEntry Add(ulong number, DateTime dateTime)
        {
            return Add(new BlockTimeEntry(number, dateTime));
        }

        public BlockTimeEntry Add(BlockTimeEntry entry)
        {
            var bucket = GetBucket(entry.BlockNumber);
            var entries = bucket.Entries;
            if (!entries.ContainsKey(entry.BlockNumber))
            {
                entries.Add(entry.BlockNumber, entry.Utc);
                store.Save(GetBucketNumber(entry.BlockNumber), bucket);
            }

            var utc = entries[entry.BlockNumber];
            var newEntry = new BlockTimeEntry(entry.BlockNumber, utc);
            if (Current != null && newEntry.BlockNumber > Current.BlockNumber) Current = newEntry;
            if (Earliest != null && newEntry.BlockNumber < Earliest.BlockNumber) Earliest = newEntry;
            return newEntry;
        }

        public BlockTimeEntry? Get(ulong number)
        {
            var bucket = GetBucket(number);
            var entries = bucket.Entries;
            if (!entries.TryGetValue(number, out DateTime utc)) return null;
            return new BlockTimeEntry(number, utc);
        }

        private BlockBucket GetBucket(ulong blockNumber)
        {
            if (buckets.Count > MaxBuckets)
            {
                buckets.Clear();
            }

            var bucketNumber = GetBucketNumber(blockNumber);
            if (!buckets.ContainsKey(bucketNumber))
            {
                var loaded = store.Load(bucketNumber);
                buckets.Add(bucketNumber, loaded);
            }
            return buckets[bucketNumber];
        }

        private ulong GetBucketNumber(ulong blockNumber)
        {
            return blockNumber / 4096;
        }
    }

    [Serializable]
    public class BlockBucket
    {
        public Dictionary<ulong, DateTime> Entries { get; set; } = new Dictionary<ulong, DateTime>();
    }

    public interface IBlockBucketStore
    {
        void Save(ulong bucketNumber, BlockBucket bucket);
        BlockBucket Load(ulong bucketNumber);
    }

    public class MemoryBlockBucketStore : IBlockBucketStore
    {
        private readonly Dictionary<ulong, BlockBucket> buckets = new Dictionary<ulong, BlockBucket>();

        public BlockBucket Load(ulong bucketNumber)
        {
            if (buckets.ContainsKey(bucketNumber))
            {
                return buckets[bucketNumber];
            }
            return new BlockBucket();
        }

        public void Save(ulong bucketNumber, BlockBucket bucket)
        {
            buckets[bucketNumber] = bucket;
        }
    }

    public class DiskBlockBucketStore : IBlockBucketStore
    {
        private readonly string dataDir;

        public DiskBlockBucketStore(string dataDir)
        {
            this.dataDir = dataDir;
            Directory.CreateDirectory(dataDir);
        }

        public BlockBucket Load(ulong bucketNumber)
        {
            var filename = ToFilename(bucketNumber);
            if (!File.Exists(filename)) return new BlockBucket();
            return JsonConvert.DeserializeObject<BlockBucket>(File.ReadAllText(filename));
        }

        public void Save(ulong bucketNumber, BlockBucket bucket)
        {
            var filename = ToFilename(bucketNumber);
            File.WriteAllText(filename, JsonConvert.SerializeObject(bucket));
        }

        private string ToFilename(ulong number)
        {
            return Path.Join(dataDir, $"{number}.json");
        }
    }
}
