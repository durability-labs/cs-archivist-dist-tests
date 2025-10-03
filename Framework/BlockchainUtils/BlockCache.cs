using Logging;
using Newtonsoft.Json;
using System.Globalization;

namespace BlockchainUtils
{
    public class BlockCache
    {
        private static object _lock = new object();
        private const int MaxBuckets = 10;
        private readonly Dictionary<ulong, BlockBucket> buckets = new Dictionary<ulong, BlockBucket>();
        private readonly BlockLadder ladder;
        private readonly IBlockBucketStore store;

        public BlockCache(ILog log)
            : this(log, new MemoryBlockBucketStore())
        {
        }

        public BlockCache(ILog log, IBlockBucketStore store)
        {
            this.store = store;

            ladder = new BlockLadder();

            log.Log("Initializing BlockCache...");
            Stopwatch.Measure(log, nameof(InitializeLadder), InitializeLadder);
        }

        public BlockTimeEntry? Earliest { get; private set; } = null;
        public BlockTimeEntry? Current { get; private set; } = null;

        public IBlockLadder Ladder => ladder;

        public BlockTimeEntry Add(ulong number, DateTime dateTime)
        {
            return Add(new BlockTimeEntry(number, dateTime));
        }

        public BlockTimeEntry Add(BlockTimeEntry entry)
        {
            lock (_lock)
            {
                var bucket = GetBucketByBlockNumber(entry.BlockNumber);
                var entries = bucket.Entries;
                if (!entries.ContainsKey(entry.BlockNumber))
                {
                    entries.Add(entry.BlockNumber, entry.Utc);
                    store.Save(GetBucketNumber(entry.BlockNumber), bucket);
                }

                var utc = entries[entry.BlockNumber];
                var newEntry = new BlockTimeEntry(entry.BlockNumber, utc);
                UpdateEarliestLatest(newEntry);
                ladder.Add(newEntry);
                return newEntry;
            }
        }

        public BlockTimeEntry? Get(ulong number)
        {
            lock (_lock)
            {
                var bucket = GetBucketByBlockNumber(number);
                var entries = bucket.Entries;
                if (!entries.TryGetValue(number, out DateTime utc)) return null;
                return new BlockTimeEntry(number, utc);
            }
        }

        private void InitializeLadder()
        {
            lock (_lock)
            {
                var bucketNumbers = store.GetBucketNumbers().Order().ToArray();
                foreach (var n in bucketNumbers)
                {
                    var bucket = GetBucketByBucketNumber(n);
                    foreach (var entry in bucket.Entries)
                    {
                        ladder.Add(new BlockTimeEntry(entry.Key, entry.Value));
                    }
                }
            }
        }

        private BlockBucket GetBucketByBlockNumber(ulong blockNumber)
        {
            return GetBucketByBucketNumber(GetBucketNumber(blockNumber));
        }

        private BlockBucket GetBucketByBucketNumber(ulong bucketNumber)
        {
            if (buckets.Count > MaxBuckets)
            {
                buckets.Clear();
            }

            if (!buckets.ContainsKey(bucketNumber))
            {
                var loaded = store.Load(bucketNumber);
                AdjustEarliestLatest(loaded);
                buckets.Add(bucketNumber, loaded);
            }
            return buckets[bucketNumber];
        }

        private void AdjustEarliestLatest(BlockBucket loaded)
        {
            foreach (var entry in loaded.Entries)
            {
                UpdateEarliestLatest(new BlockTimeEntry(entry.Key, entry.Value));
            }
        }

        private void UpdateEarliestLatest(BlockTimeEntry newEntry)
        {
            if (Current == null || newEntry.BlockNumber > Current.BlockNumber) Current = newEntry;
            if (Earliest == null || newEntry.BlockNumber < Earliest.BlockNumber) Earliest = newEntry;
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
        ulong[] GetBucketNumbers();
        void Save(ulong bucketNumber, BlockBucket bucket);
        BlockBucket Load(ulong bucketNumber);
    }

    public class MemoryBlockBucketStore : IBlockBucketStore
    {
        private readonly Dictionary<ulong, BlockBucket> buckets = new Dictionary<ulong, BlockBucket>();

        public ulong[] GetBucketNumbers()
        {
            return buckets.Keys.ToArray();
        }

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

        public ulong[] GetBucketNumbers()
        {
            var result = new List<ulong>();
            var files = Directory.GetFiles(dataDir);
            foreach (var file in files)
            {
                var name = Path.GetFileName(file);
                if (name.EndsWith(".json"))
                {
                    if (ulong.TryParse(name.Substring(0, name.Length - 5), CultureInfo.InvariantCulture, out ulong n))
                    {
                        result.Add(n);
                    }
                }
            }
            return result.ToArray();
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
            return Path.Join(dataDir, $"{number.ToString(CultureInfo.InvariantCulture)}.json");
        }
    }
}
