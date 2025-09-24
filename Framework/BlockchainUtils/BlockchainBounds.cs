using Logging;
using Utils;

namespace BlockchainUtils
{
    public interface IWeb3Blocks
    {
        ulong GetCurrentBlockNumber();
        DateTime? GetTimestampForBlock(ulong blockNumber);
    }

    public class BlockchainBounds
    {
        private readonly ILog log;
        private readonly BlockCache cache;
        private readonly IWeb3Blocks web3;

        public BlockchainBounds(ILog log, BlockCache cache, IWeb3Blocks web3)
        {
            this.log = log;
            this.cache = cache;
            this.web3 = web3;
        }

        public BlockTimeEntry Earliest => cache.Earliest;
        public BlockTimeEntry Current => cache.Current;

        public void Initialize()
        {
            AddCurrentBlock();
            LookForEarliest();

            if (Current.BlockNumber == Earliest.BlockNumber)
            {
                throw new Exception("Unsupported condition: Current block is earliest block.");
            }
        }

        public void UpdateCurrentIfNeeded(ulong newNumber)
        {
            if (newNumber > Current.BlockNumber) AddCurrentBlock();
        }

        public void UpdateCurrentIfNeeded(DateTime utc)
        {
            if (utc > Current.Utc) AddCurrentBlock();
        }

        private void LookForEarliest()
        {
            if (Earliest != null)
            {
                cache.Add(Earliest);
                return;
            }

            LookForEarliestBlock(0, Current.BlockNumber);
        }

        private void LookForEarliestBlock(ulong lower, ulong upper)
        {
            if (Earliest != null) return;

            var blockTime = GetBlockTime(lower);
            if (blockTime != null && IsValid(blockTime))
            {
                AddEarliestBlock(lower, blockTime.Value);
                return;
            }

            var range = upper - lower;
            if (range < 2)
            {
                var upperTime = GetBlockTime(upper);
                if (upperTime == null || !IsValid(upperTime)) throw new Exception("Became invalid during function call.");
                AddEarliestBlock(upper, upperTime.Value);
                return;
            }

            var middle = lower + range / 2;

            var middleTime = GetBlockTime(middle);
            if (middleTime != null && IsValid(middleTime))
            {
                LookForEarliestBlock(lower, middle);
                if (Earliest == null)
                {
                    AddEarliestBlock(middle, middleTime.Value);
                    return;
                }
            }
            else
            {
                LookForEarliestBlock(middle, upper);
            }
        }

        private void AddCurrentBlock()
        {
            var currentBlockNumber = web3.GetCurrentBlockNumber();
            if (Current != null && Current.BlockNumber == currentBlockNumber) return;

            var blockTime = GetBlockTime(currentBlockNumber);
            if (blockTime == null) throw new Exception("Unable to get dateTime for current block.");
            if (!IsValid(blockTime)) throw new Exception("Received invalid dateTime for current block.");
            AddCurrentBlock(currentBlockNumber, blockTime.Value);
        }

        private void AddCurrentBlock(ulong currentBlockNumber, DateTime dateTime)
        {
            var entry = new BlockTimeEntry(currentBlockNumber, dateTime);
            log.Log($"Current block: {entry}");
            cache.SetCurrent(entry);
            cache.Add(Current);
        }

        private void AddEarliestBlock(ulong number, DateTime dateTime)
        {
            AddEarliestBlock(new BlockTimeEntry(number, dateTime));
        }

        private void AddEarliestBlock(BlockTimeEntry entry)
        {
            log.Log($"Earliest block: {entry}");
            cache.SetEarliest(entry);
            cache.Add(Earliest);
        }

        private DateTime? GetBlockTime(ulong number)
        {
            var entry = cache.Get(number);
            if (entry != null) return entry.Utc;

            var blockTime = web3.GetTimestampForBlock(number);
            if (blockTime != null)
            {
                cache.Add(number, blockTime.Value);
                return blockTime.Value;
            }
            return null;
        }

        private bool IsValid(DateTime? blockTime)
        {
            if (blockTime == null) return false;
            var timestamp = Time.ToUnixTimeSeconds(blockTime.Value);
            return timestamp > 1;
        }
    }
}
