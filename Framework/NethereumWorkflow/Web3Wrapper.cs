using BlockchainUtils;
using Logging;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Utils;

namespace NethereumWorkflow
{
    public class Web3Wrapper : IWeb3Blocks
    {
        private readonly Web3 web3;
        private readonly ILog log;
        private ulong? earliest;
        private ulong? latest;

        public Web3Wrapper(Web3 web3, ILog log)
        {
            this.web3 = web3;
            this.log = log;
        }

        public ulong GetCurrentBlockNumber()
        {
            return Retry(() =>
            {
                var number = Time.Wait(web3.Eth.Blocks.GetBlockNumber.SendRequestAsync());
                var result = Convert.ToUInt64(number.ToDecimal());
                if (earliest == null || result < earliest) earliest = result;
                if (latest == null || result > latest) latest = result;
                return result;
            });
        }

        public ulong? GetEarliestSeen()
        {
            return earliest;
        }

        public ulong? GetLatestSeen()
        {
            return latest;
        }

        public BlockTimeEntry? GetTimestampForBlock(ulong blockNumber)
        {
            return Retry(() =>
            {
                try
                {
                    if (blockNumber == 0) return null;
                    var block = Time.Wait(web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(new BlockParameter(blockNumber)));
                    if (block == null) return null;
                    var timestamp = Convert.ToInt64(block.Timestamp.ToDecimal());
                    if (timestamp < 1) return null;
                    var utc = DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;
                    return new BlockTimeEntry(blockNumber, utc);
                }
                catch (Exception ex)
                {
                    log.Error("Exception while getting timestamp for block: " + ex);
                    return null;
                }
            });
        }

        private T Retry<T>(Func<T> action)
        {
            var retry = new Retry(nameof(Web3Wrapper),
                maxTimeout: TimeSpan.FromSeconds(30),
                sleepAfterFail: TimeSpan.FromSeconds(3),
                onFail: f => { },
                failFast: false);

            return retry.Run(action);
        }
    }
}
