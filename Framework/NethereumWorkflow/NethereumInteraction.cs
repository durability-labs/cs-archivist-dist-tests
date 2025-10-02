using System.Numerics;
using BlockchainUtils;
using Logging;
using Nethereum.BlockchainProcessing.Processor;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Utils;

namespace NethereumWorkflow
{
    public class NethereumInteraction
    {
        private readonly IWeb3Blocks blocks;
        private readonly ILog log;
        private readonly Web3 web3;
        private readonly BlockCache blockCache;

        internal NethereumInteraction(ILog log, Web3 web3, BlockCache blockCache)
        {
            this.log = log;
            this.web3 = web3;
            this.blockCache = blockCache;
            var wrapper = new Web3Wrapper(web3, log);
            blocks = new CachingWeb3Wrapper(log, wrapper, blockCache);
        }

        public string SendEth(string toAddress, Ether eth)
        {
            var asDecimal = ((decimal)eth.Wei) / (decimal)TokensIntExtensions.WeiPerEth;
            return SendEth(toAddress, asDecimal);
        }

        public string SendEth(string toAddress, decimal ethAmount)
        {
            if (ethAmount == 0) return string.Empty;
            return DebugLogWrap(() =>
            {
                var receipt = Time.Wait(web3.Eth.GetEtherTransferService().TransferEtherAndWaitForReceiptAsync(toAddress, ethAmount));
                if (!receipt.Succeeded()) throw new Exception("Unable to send Eth");
                return receipt.TransactionHash;
            }, nameof(SendEth));
        }

        public BigInteger GetEthBalance()
        {
            return DebugLogWrap(() =>
            {
                return GetEthBalance(web3.TransactionManager.Account.Address);
            }, nameof(GetEthBalance));
        }

        public BigInteger GetEthBalance(string address)
        {
            return DebugLogWrap(() =>
            {
                var balance = Time.Wait(web3.Eth.GetBalance.SendRequestAsync(address));
                return balance.Value;
            }, nameof(GetEthBalance));
        }

        public TResult Call<TFunction, TResult>(string contractAddress, TFunction function) where TFunction : FunctionMessage, new()
        {
            return DebugLogWrap(() =>
            {
                var handler = web3.Eth.GetContractQueryHandler<TFunction>();
                return Time.Wait(handler.QueryAsync<TResult>(contractAddress, function));
            }, nameof(Call) + "." + typeof(TFunction).ToString());
        }

        public TResult Call<TFunction, TResult>(string contractAddress, TFunction function, ulong blockNumber) where TFunction : FunctionMessage, new()
        {
            return DebugLogWrap(() =>
            {
                var handler = web3.Eth.GetContractQueryHandler<TFunction>();
                return Time.Wait(handler.QueryAsync<TResult>(contractAddress, function, new BlockParameter(blockNumber)));
            }, nameof(Call) + "." + typeof(TFunction).ToString());
        }

        public void Call<TFunction>(string contractAddress, TFunction function) where TFunction : FunctionMessage, new()
        {
            DebugLogWrap<string>(() =>
            {
                var handler = web3.Eth.GetContractQueryHandler<TFunction>();
                Time.Wait(handler.QueryRawAsync(contractAddress, function));
                return string.Empty;
            }, nameof(Call) + "." + typeof(TFunction).ToString());
        }

        public void Call<TFunction>(string contractAddress, TFunction function, ulong blockNumber) where TFunction : FunctionMessage, new()
        {
            DebugLogWrap<string>(() =>
            {
                var handler = web3.Eth.GetContractQueryHandler<TFunction>();
                var result = Time.Wait(handler.QueryRawAsync(contractAddress, function, new BlockParameter(blockNumber)));
                return string.Empty;
            }, nameof(Call) + "." + typeof(TFunction).ToString());
        }

        public string SendTransaction<TFunction>(string contractAddress, TFunction function) where TFunction : FunctionMessage, new()
        {
            return DebugLogWrap(() =>
            {
                var handler = web3.Eth.GetContractTransactionHandler<TFunction>();
                var receipt = Time.Wait(handler.SendRequestAndWaitForReceiptAsync(contractAddress, function));
                if (!receipt.Succeeded()) throw new Exception("Unable to perform contract transaction.");
                return receipt.TransactionHash;
            }, nameof(SendTransaction) + "." + typeof(TFunction).ToString());
        }

        public Transaction GetTransaction(string transactionHash)
        {
            return DebugLogWrap(() =>
            {
                return Time.Wait(web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(transactionHash));
            }, nameof(GetTransaction));
        }

        public decimal? GetSyncedBlockNumber()
        {
            return DebugLogWrap<decimal?>(() =>
            {
                var sync = Time.Wait(web3.Eth.Syncing.SendRequestAsync());
                var number = Time.Wait(web3.Eth.Blocks.GetBlockNumber.SendRequestAsync());
                var numberOfBlocks = number.ToDecimal();
                if (sync.IsSyncing) return null;
                return numberOfBlocks;
            }, nameof(GetTransaction));
        }

        public bool IsContractAvailable(string abi, string contractAddress)
        {
            return DebugLogWrap(() =>
            {
                try
                {
                    var contract = web3.Eth.GetContract(abi, contractAddress);
                    return contract != null;
                }
                catch
                {
                    return false;
                }
            }, nameof(IsContractAvailable));
        }

        public IEventsCollector[] GetEvents(string address, BlockInterval blockRange, params IEventsCollector[] collectors)
        {
            return GetEvents(address, blockRange.From, blockRange.To, collectors);
        }

        public IFunctionCallCollector[] GetFunctionCalls(string address, BlockInterval blockRange, params IFunctionCallCollector[] collectors)
        {
            return GetCalls(address, blockRange.From, blockRange.To, collectors);
        }

        public BlockTimeEntry? GetBlockForNumber(ulong number)
        {
            return blocks.GetTimestampForBlock(number);
        }

        public BlockTimeEntry? GetBlockForUtc(DateTime utc)
        {
            return DebugLogWrap(() =>
            {
                var blockTimeFinder = new BlockTimeFinder(blocks, log, blockCache.Ladder);
                return blockTimeFinder.GetHighestBlockNumberBefore(utc);
            }, nameof(GetBlockForUtc));
        }

        private IEventsCollector[] GetEvents(string address, ulong fromBlockNumber, ulong toBlockNumber, params IEventsCollector[] collectors)
        {
            return DebugLogWrap(() =>
            {
                var logs = new List<FilterLog>();
                var p = web3.Processing.Logs.CreateProcessorForContract(
                    address,
                    action: logs.Add,
                    minimumBlockConfirmations: 1,
                    criteria: l =>
                    {
                        foreach (var c in collectors)
                        {
                            if (c.AbiEvent.IsLogForEvent(l)) return true;
                        }
                        return false;
                    }
                );

                var from = new BlockParameter(fromBlockNumber);
                var to = new BlockParameter(toBlockNumber);
                var ct = new CancellationTokenSource().Token;
                Time.Wait(p.ExecuteAsync(toBlockNumber: to.BlockNumber, cancellationToken: ct, startAtBlockNumberIfNotProcessed: from.BlockNumber));

                foreach (var t in collectors)
                {
                    t.CollectMyEvents(logs);
                }

                return collectors;
            }, $"{nameof(GetEvents)}<{string.Join(",", collectors.Select(c => c.Name).ToArray())}>[{fromBlockNumber} -> {toBlockNumber}]");
        }

        private IFunctionCallCollector[] GetCalls(string address, ulong fromBlockNumber, ulong toBlockNumber, params IFunctionCallCollector[] collectors)
        {
            return DebugLogWrap(() =>
            {
                var p = web3.Processing.Blocks.CreateBlockProcessor(a =>
                {
                    a.TransactionStep.SetMatchCriteria(t => 
                        t.Transaction.IsTo(address) &&
                        IsFunctionCallForAnyCollector(t.Transaction, collectors)
                    );

                    a.TransactionStep.AddSynchronousProcessorHandler(t =>
                    {
                        foreach (var c in collectors)
                        {
                            if (c.IsFunction(t.Transaction))
                            {
                                var timestamp = Convert.ToInt64(t.Block.Timestamp.ToDecimal());
                                var utc = DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;
                                var block = blockCache.Add(t.Block.Number.ToUlong(), utc);
                                c.AddCall(block, t.Transaction);
                            }
                        }
                    });
                });

                var from = new BlockParameter(fromBlockNumber);
                var to = new BlockParameter(toBlockNumber);
                var ct = new CancellationTokenSource().Token;
                Time.Wait(p.ExecuteAsync(toBlockNumber: to.BlockNumber, cancellationToken: ct, startAtBlockNumberIfNotProcessed: from.BlockNumber));

                return collectors;
            }, $"{nameof(GetCalls)}<{string.Join(",", collectors.Select(c => c.Name).ToArray())}>[{fromBlockNumber} -> {toBlockNumber}]");
        }

        private static bool IsFunctionCallForAnyCollector(Transaction transaction, IFunctionCallCollector[] collectors)
        {
            foreach (var c in collectors)
            {
                if (c.IsFunction(transaction)) return true;
            }
            return false;
        }

        private T DebugLogWrap<T>(Func<T> task, string name = "")
        {
            return Stopwatch.Measure(log, name, task, debug: true).Value;
        }
    }
}
