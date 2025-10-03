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
            if (blockNumber == 0) return null;

            var timeout = DateTime.UtcNow + TimeSpan.FromSeconds(90);
            while (DateTime.UtcNow < timeout)
            {
                var block = GetBlockTimestamp(blockNumber);
                if (block != null)
                {
                    var timestamp = Convert.ToInt64(block.Timestamp.ToDecimal());
                    if (timestamp > 0)
                    {
                        var utc = DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;
                        return new BlockTimeEntry(blockNumber, utc);
                    }
                }

                Thread.Sleep(TimeSpan.FromSeconds(3));
            }
            throw new TimeoutException($"{nameof(GetTimestampForBlock)} {blockNumber}");
        }

        private BlockWithTransactions GetBlockTimestamp(ulong blockNumber)
        {
            return Retry(() =>
            {
                return Time.Wait(web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(new BlockParameter(blockNumber)));
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
