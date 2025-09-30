using Logging;
using Nethereum.RPC.Eth.DTOs;

namespace BlockchainUtils
{
    public interface IWeb3Blocks
    {
        ulong GetCurrentBlockNumber();
        BlockTimeEntry? GetTimestampForBlock(ulong blockNumber);
        ulong? GetEarliestSeen();
        ulong? GetLatestSeen();
        BlockWithTransactions GetBlockWithTransactions(ulong number);
    }

    public class BlockchainBounds
    {
        private readonly ILog log;
        private readonly IWeb3Blocks web3;

        public BlockchainBounds(ILog log, IWeb3Blocks web3)
        {
            this.log = log;
            this.web3 = web3;
        }

        public void InitializeBounds()
        {
            LogCurrentBlock();
            LookForEarliest();

            if (web3.GetEarliestSeen() == web3.GetLatestSeen())
            {
                throw new Exception("Unsupported condition: Earliest block is latest block.");
            }
        }

        public BlockTimeEntry Current
        {
            get
            {
                return web3.GetTimestampForBlock(EnsureInit(web3.GetLatestSeen()))!;
            }
        }

        public BlockTimeEntry Earliest
        {
            get
            {
                return web3.GetTimestampForBlock(EnsureInit(web3.GetEarliestSeen()))!;
            }
        }

        private ulong EnsureInit(ulong? num)
        {
            if (num == null) throw new Exception("BlockchainBounds not initialized");
            return num.Value;
        }

        private void LookForEarliest()
        {
            LookForEarliestBlock(1, web3.GetCurrentBlockNumber());
        }

        private bool LookForEarliestBlock(ulong lower, ulong upper)
        {
            var blockTime = GetBlockTime(lower);
            if (blockTime != null)
            {
                LogEarliestBlock(lower, blockTime.Value);
                return true;
            }

            var range = upper - lower;
            if (range < 2)
            {
                var upperTime = GetBlockTime(upper);
                if (upperTime == null) throw new Exception("Became invalid during function call.");
                LogEarliestBlock(upper, upperTime.Value);
                return true;
            }

            var middle = lower + range / 2;
            var middleTime = GetBlockTime(middle);
            if (middleTime != null)
            {
                var success = LookForEarliestBlock(lower, middle);
                if (success) return true;
                
                LogEarliestBlock(middle, middleTime.Value);
                return true;
            }
         
            return LookForEarliestBlock(middle, upper);
        }

        private DateTime? GetBlockTime(ulong number)
        {
            return web3.GetTimestampForBlock(number)?.Utc;
        }

        private void LogCurrentBlock()
        {
            var currentBlockNumber = web3.GetCurrentBlockNumber();
            var blockTime = GetBlockTime(currentBlockNumber);
            if (blockTime == null) throw new Exception("Unable to get dateTime for current block.");
            LogCurrentBlock(currentBlockNumber, blockTime.Value);
        }

        private void LogCurrentBlock(ulong currentBlockNumber, DateTime dateTime)
        {
            var entry = new BlockTimeEntry(currentBlockNumber, dateTime);
            log.Debug($"Current block: {entry}");
        }

        private void LogEarliestBlock(ulong number, DateTime dateTime)
        {
            LogEarliestBlock(new BlockTimeEntry(number, dateTime));
        }

        private void LogEarliestBlock(BlockTimeEntry entry)
        {
            log.Debug($"Earliest block: {entry}");
        }
    }
}
