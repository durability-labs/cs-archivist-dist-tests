using BlockchainUtils;
using Logging;

namespace NethereumWorkflow
{
    public class CachingWeb3Wrapper : IWeb3Blocks
    {
        private readonly ILog log;
        private readonly IWeb3Blocks backingWeb3;
        private readonly BlockCache cache;

        public CachingWeb3Wrapper(ILog log, IWeb3Blocks backingWeb3, BlockCache cache)
        {
            this.log = log;
            this.backingWeb3 = backingWeb3;
            this.cache = cache;
        }

        public ulong GetCurrentBlockNumber()
        {
            var number = backingWeb3.GetCurrentBlockNumber();
            GetTimestampForBlock(number);
            return number;
        }

        public ulong? GetEarliestSeen()
        {
            return cache.Earliest?.BlockNumber;
        }

        public ulong? GetLatestSeen()
        {
            return cache.Current?.BlockNumber;
        }

        public BlockTimeEntry? GetTimestampForBlock(ulong blockNumber)
        {
            var entry = cache.Get(blockNumber);
            if (entry != null)
            {
                log.Debug("\t\t\tfrom cache: " + blockNumber);
                return entry;
            }

            var blockTime = backingWeb3.GetTimestampForBlock(blockNumber);
            if (blockTime != null)
            {
                cache.Add(blockTime);
                log.Debug("\t\t\t\t\t\tactual fetch: " + blockNumber);
                return blockTime;
            }
            return null;
        }
    }
}
