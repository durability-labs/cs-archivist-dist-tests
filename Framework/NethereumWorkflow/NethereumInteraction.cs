using BlockchainUtils;
using Logging;
using Nethereum.BlockchainProcessing.Processor;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using System.Numerics;
using Utils;

namespace NethereumWorkflow
{
    public class NethereumInteraction
    {
        private static readonly object web3Lock = new object();
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
            return LockWrap(() =>
            {
                var receipt = Time.Wait(web3.Eth.GetEtherTransferService().TransferEtherAndWaitForReceiptAsync(toAddress, ethAmount));
                if (!receipt.Succeeded()) throw new Exception("Unable to send Eth");
                return receipt.TransactionHash;
            }, nameof(SendEth));
        }

        public BigInteger GetEthBalance()
        {
            return LockWrap(() =>
            {
                return GetEthBalance(web3.TransactionManager.Account.Address);
            }, nameof(GetEthBalance));
        }

        public BigInteger GetEthBalance(string address)
        {
            return LockWrap(() =>
            {
                var balance = Time.Wait(web3.Eth.GetBalance.SendRequestAsync(address));
                return balance.Value;
            }, nameof(GetEthBalance));
        }

        public TResult Call<TFunction, TResult>(ContractAddress contractAddress, TFunction function) where TFunction : FunctionMessage, new()
        {
            return LockWrap(() =>
            {
                var handler = web3.Eth.GetContractQueryHandler<TFunction>();
                return Time.Wait(handler.QueryAsync<TResult>(contractAddress.Address, function));
            }, nameof(Call) + "." + typeof(TFunction).ToString());
        }

        public TResult Call<TFunction, TResult>(ContractAddress contractAddress, TFunction function, ulong blockNumber) where TFunction : FunctionMessage, new()
        {
            return LockWrap(() =>
            {
                var handler = web3.Eth.GetContractQueryHandler<TFunction>();
                return Time.Wait(handler.QueryAsync<TResult>(contractAddress.Address, function, new BlockParameter(blockNumber)));
            }, nameof(Call) + "." + typeof(TFunction).ToString());
        }

        public void Call<TFunction>(ContractAddress contractAddress, TFunction function) where TFunction : FunctionMessage, new()
        {
            LockWrap<string>(() =>
            {
                var handler = web3.Eth.GetContractQueryHandler<TFunction>();
                Time.Wait(handler.QueryRawAsync(contractAddress.Address, function));
                return string.Empty;
            }, nameof(Call) + "." + typeof(TFunction).ToString());
        }

        public void Call<TFunction>(ContractAddress contractAddress, TFunction function, ulong blockNumber) where TFunction : FunctionMessage, new()
        {
            LockWrap<string>(() =>
            {
                var handler = web3.Eth.GetContractQueryHandler<TFunction>();
                var result = Time.Wait(handler.QueryRawAsync(contractAddress.Address, function, new BlockParameter(blockNumber)));
                return string.Empty;
            }, nameof(Call) + "." + typeof(TFunction).ToString());
        }

        public string SendTransaction<TFunction>(ContractAddress contractAddress, TFunction function) where TFunction : FunctionMessage, new()
        {
            return LockWrap(() =>
            {
                var handler = web3.Eth.GetContractTransactionHandler<TFunction>();
                var receipt = Time.Wait(handler.SendRequestAndWaitForReceiptAsync(contractAddress.Address, function));
                if (!receipt.Succeeded()) throw new Exception("Unable to perform contract transaction.");
                return receipt.TransactionHash;
            }, nameof(SendTransaction) + "." + typeof(TFunction).ToString());
        }

        public Transaction GetTransaction(string transactionHash)
        {
            return LockWrap(() =>
            {
                return Time.Wait(web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(transactionHash));
            }, nameof(GetTransaction));
        }

        public decimal? GetSyncedBlockNumber()
        {
            return LockWrap<decimal?>(() =>
            {
                var sync = Time.Wait(web3.Eth.Syncing.SendRequestAsync());
                var number = Time.Wait(web3.Eth.Blocks.GetBlockNumber.SendRequestAsync());
                var numberOfBlocks = number.ToDecimal();
                if (sync.IsSyncing) return null;
                return numberOfBlocks;
            }, nameof(GetTransaction));
        }

        public bool IsContractAvailable(string abi, ContractAddress contractAddress)
        {
            return LockWrap(() =>
            {
                try
                {
                    var contract = web3.Eth.GetContract(abi, contractAddress.Address);
                    return contract != null;
                }
                catch
                {
                    return false;
                }
            }, nameof(IsContractAvailable));
        }

        public IEventsCollector[] GetEvents(ContractAddress address, BlockInterval blockRange, params IEventsCollector[] collectors)
        {
            return GetEvents(address, blockRange.From, blockRange.To, collectors);
        }

        public IFunctionCallCollector[] GetFunctionCalls(ContractAddress address, BlockInterval blockRange, params IFunctionCallCollector[] collectors)
        {
            return GetCalls(address, blockRange.From, blockRange.To, collectors);
        }

        public BlockTimeEntry? GetBlockForNumber(ulong number)
        {
            return blocks.GetTimestampForBlock(number);
        }

        public BlockTimeEntry GetHighestBlockBeforeUtc(DateTime utc)
        {
            return LockWrap(() =>
            {
                var blockTimeFinder = new BlockTimeFinder(blocks, log, blockCache.Ladder);
                return blockTimeFinder.GetHighestBlockNumberBefore(utc);
            }, nameof(GetHighestBlockBeforeUtc));
        }

        public BlockTimeEntry GetLowestBlockAfterUtc(DateTime utc)
        {
            return LockWrap(() =>
            {
                var blockTimeFinder = new BlockTimeFinder(blocks, log, blockCache.Ladder);
                return blockTimeFinder.GetLowestBlockNumberAfter(utc);
            }, nameof(GetLowestBlockAfterUtc));
        }

        private IEventsCollector[] GetEvents(ContractAddress address, ulong fromBlockNumber, ulong toBlockNumber, params IEventsCollector[] collectors)
        {
            var context = $"{nameof(NethereumInteraction)}.{nameof(GetEvents)}";
            return LockWrap(() =>
            {
                var logs = new List<FilterLog>();
                var p = web3.Processing.Logs.CreateProcessorForContract(
                    address.Address,
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
                Stopwatch.Measure(log, $"{context}.ExecuteAsync", () =>
                {
                    Time.Wait(p.ExecuteAsync(toBlockNumber: to.BlockNumber, cancellationToken: ct, startAtBlockNumberIfNotProcessed: from.BlockNumber));
                }, true);

                foreach (var t in collectors)
                {
                    t.CollectMyEvents(logs);
                }

                return collectors;
            }, $"{nameof(GetEvents)}<{string.Join(",", collectors.Select(c => c.Name).ToArray())}>[{fromBlockNumber} -> {toBlockNumber}]");
        }

        private IFunctionCallCollector[] GetCalls(ContractAddress address, ulong fromBlockNumber, ulong toBlockNumber, params IFunctionCallCollector[] collectors)
        {
            return LockWrap(() =>
            {
                var progressLogger = new ProgressLogger(new LogPrefixer(log, "(FunctionCallProcessor)"), fromBlockNumber, toBlockNumber);
                var p = web3.Processing.Blocks.CreateBlockProcessor(a =>
                {
                    a.TransactionStep.SetMatchCriteria(t =>
                    {
                        progressLogger.Progress(t.Block.Number.ToUlong());
                        return
                            t.Transaction.IsTo(address.Address) &&
                            IsFunctionCallForAnyCollector(t.Transaction, collectors);
                        }
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

        private T LockWrap<T>(Func<T> task, string name = "")
        {
            lock (web3Lock)
            {
                return Stopwatch.Measure(log, name, task, debug: true).Value;
            }
        }

        private class ProgressLogger
        {
            private readonly ILog log;
            private readonly ulong from;
            private readonly ulong to;
            private DateTime utc;

            public ProgressLogger(ILog log, ulong from, ulong to)
            {
                this.log = log;
                this.from = from;
                this.to = to;

                utc = DateTime.UtcNow;
            }

            public void Progress(ulong current)
            {
                if (DateTime.UtcNow - utc > TimeSpan.FromSeconds(10.0))
                {
                    utc = DateTime.UtcNow;

                    var factor = GetFactor(current);
                    var line = $"[{from}] (";
                    line += Repeat("-", factor * 30.0f);
                    line += Repeat(" ", 30.0f - (factor * 30.0f));
                    line += $") [{to}]";
                    log.Log(line);
                }
            }

            private static string Repeat(string str, float count)
            {
                var c = Convert.ToInt32(Math.Round(count));
                var result = "";
                for (var i = 0; i < c; i++) result += str;
                return result;
            }

            private float GetFactor(ulong current)
            {
                var range = Convert.ToSingle(to - from);
                var progress = Convert.ToSingle(current - from);
                var result = progress / range;
                result = Math.Clamp(result, 0.001f, 1.0f);
                return result;
            }
        }
    }
}
