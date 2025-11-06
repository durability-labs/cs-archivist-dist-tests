using BlockchainUtils;
using ArchivistContractsPlugin.Marketplace;
using GethPlugin;
using Logging;
using Nethereum.ABI;
using Nethereum.Contracts;
using Nethereum.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Utils;

namespace ArchivistContractsPlugin
{
    public interface IArchivistContracts
    {
        ArchivistContractsDeployment Deployment { get; }

        bool IsDeployed();
        string MintTestTokens(IHasEthAddress owner, TestToken testTokens);
        string MintTestTokens(EthAddress ethAddress, TestToken testTokens);
        void ApproveTestTokens(EthAddress ethAddress, TestToken testTokens);
        TestToken GetTestTokenBalance(IHasEthAddress owner);
        TestToken GetTestTokenBalance(EthAddress ethAddress);
        string TransferTestTokens(EthAddress to, TestToken amount);

        IArchivistContractsEvents GetEvents(TimeRange timeRange);
        IArchivistContractsEvents GetEvents(BlockInterval blockInterval);
        EthAddress? GetSlotHost(byte[] requestId, decimal slotIndex);
        RequestState GetRequestState(byte[] requestId);
        Request GetRequest(byte[] requestId);
        ulong GetPeriodNumber(DateTime utc);
        TimeRange GetPeriodTimeRange(ulong periodNumber);
        void WaitUntilNextPeriod();
        bool IsProofRequired(byte[] requestId, decimal slotIndex);
        bool WillProofBeRequired(byte[] requestId, decimal slotIndex);
        byte[] GetSlotId(byte[] requestId, decimal slotIndex);
        bool CanMarkProofAsMissing(byte[] slotId, ulong period);

        IArchivistContracts WithDifferentGeth(IGethNode node);
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum RequestState
    {
        New,
        Started,
        Cancelled,
        Finished,
        Failed
    }

    public class ArchivistContractsAccess : IArchivistContracts
    {
        private readonly ILog log;
        private readonly IGethNode gethNode;
        private readonly IRequestsCache requestsCache;
        private ContractAddress? tokenAddress = null;

        public ArchivistContractsAccess(ILog log, IGethNode gethNode, ArchivistContractsDeployment deployment, IRequestsCache requestsCache)
        {
            this.log = log;
            this.gethNode = gethNode;
            Deployment = deployment;
            this.requestsCache = requestsCache;
        }

        public ArchivistContractsDeployment Deployment { get; }

        public bool IsDeployed()
        {
            try
            {
                GetTokenAddress();
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Failed to fetch tokenAddress: {ex}");
                return false;
            }
        }

        private ContractAddress GetTokenAddress()
        {
            if (tokenAddress == null)
            {
                tokenAddress = StartInteraction().GetTokenAddress(Deployment.MarketplaceAddress);
            }
            return tokenAddress;
        }

        public string MintTestTokens(IHasEthAddress owner, TestToken testTokens)
        {
            return MintTestTokens(owner.EthAddress, testTokens);
        }

        public string MintTestTokens(EthAddress ethAddress, TestToken testTokens)
        {
            return StartInteraction().MintTestTokens(ethAddress, testTokens.TstWei, GetTokenAddress());
        }

        public void ApproveTestTokens(EthAddress ethAddress, TestToken testTokens)
        {
            StartInteraction().ApproveTestTokens(GetTokenAddress(), ethAddress, testTokens);
        }

        public TestToken GetTestTokenBalance(IHasEthAddress owner)
        {
            return GetTestTokenBalance(owner.EthAddress);
        }

        public TestToken GetTestTokenBalance(EthAddress ethAddress)
        {
            var balance = StartInteraction().GetBalance(GetTokenAddress(), ethAddress.Address);
            return balance.TstWei();
        }

        public string TransferTestTokens(EthAddress to, TestToken amount)
        {
            return StartInteraction().TransferTestTokens(GetTokenAddress(), to.Address, amount.TstWei);
        }

        public IArchivistContractsEvents GetEvents(TimeRange timeRange)
        {
            return GetEvents(new BlockInterval(timeRange,
                gethNode.GetBlockForUtc(timeRange.From)!.BlockNumber,
                gethNode.GetBlockForUtc(timeRange.To)!.BlockNumber));
        }

        public IArchivistContractsEvents GetEvents(BlockInterval blockInterval)
        {
            return new ArchivistContractsEvents(log, gethNode, Deployment, blockInterval);
        }

        public EthAddress? GetSlotHost(byte[] requestId, decimal slotIndex)
        {
            var slotId = GetSlotId(requestId, slotIndex);
            var func = new GetHostFunction
            {
                SlotId = slotId
            };
            var address = gethNode.Call<GetHostFunction, string>(Deployment.MarketplaceAddress, func);
            if (string.IsNullOrEmpty(address)) return null;
            return new EthAddress(address);
        }

        public RequestState GetRequestState(byte[] requestId)
        {
            if (requestId == null) throw new ArgumentNullException(nameof(requestId));
            if (requestId.Length != 32) throw new InvalidDataException(nameof(requestId) + $"{nameof(requestId)} length should be 32 bytes, but was: {requestId.Length}" + requestId.Length);

            var func = new RequestStateFunction
            {
                RequestId = requestId
            };
            return gethNode.Call<RequestStateFunction, RequestState>(Deployment.MarketplaceAddress, func);
        }

        public Request GetRequest(byte[] requestId)
        {
            var cached = requestsCache.Get(requestId);
            if (cached != null) return cached;

            if (requestId == null) throw new ArgumentNullException(nameof(requestId));
            if (requestId.Length != 32) throw new InvalidDataException(nameof(requestId) + $"{nameof(requestId)} length should be 32 bytes, but was: {requestId.Length}" + requestId.Length);
            var func = new GetRequestFunction
            {
                RequestId = requestId
            };

            var request = gethNode.Call<GetRequestFunction, GetRequestOutputDTO>(Deployment.MarketplaceAddress, func);
            var result = request.ReturnValue1;
            requestsCache.Add(requestId, result);
            return result;
        }

        public ulong GetPeriodNumber(DateTime utc)
        {
            var now = Time.ToUnixTimeSeconds(utc);
            var periodSeconds = (int)Deployment.Config.Proofs.Period;
            var result = now / periodSeconds;
            return Convert.ToUInt64(result);
        }

        public TimeRange GetPeriodTimeRange(ulong periodNumber)
        {
            var periodSeconds = Deployment.Config.Proofs.Period;
            var startUtco = Convert.ToInt64(periodSeconds * periodNumber);
            var endUtco = Convert.ToInt64(periodSeconds * (periodNumber + 1));
            var start = Time.ToUtcDateTime(startUtco);
            var end = Time.ToUtcDateTime(endUtco);
            return new TimeRange(start, end);
        }

        public void WaitUntilNextPeriod()
        {
            Thread.Sleep(TimeSpan.FromSeconds(1.0));
            var timeRange = GetPeriodTimeRange(GetPeriodNumber(DateTime.UtcNow));
            var span = TimeSpan.FromSeconds(1.0) + (timeRange.To - DateTime.UtcNow);
            log.Log($"Waiting until next period: {Time.FormatDuration(span)}");
            Thread.Sleep(span);
        }

        public bool IsProofRequired(byte[] requestId, decimal slotIndex)
        {
            var slotId = GetSlotId(requestId, slotIndex);
            return IsProofRequired(slotId);
        }

        public bool WillProofBeRequired(byte[] requestId, decimal slotIndex)
        {
            var slotId = GetSlotId(requestId, slotIndex);
            return WillProofBeRequired(slotId);
        }

        public IArchivistContracts WithDifferentGeth(IGethNode node)
        {
            return new ArchivistContractsAccess(log, node, Deployment, requestsCache);
        }

        public byte[] GetSlotId(byte[] requestId, decimal slotIndex)
        {
            var encoder = new ABIEncode();
            var encoded = encoder.GetABIEncoded(
                new ABIValue("bytes32", requestId),
                new ABIValue("uint256", slotIndex.ToBig())
            );

            return Sha3Keccack.Current.CalculateHash(encoded);
        }

        public bool CanMarkProofAsMissing(byte[] slotId, ulong period)
        {
            var func = new CanMarkProofAsMissingFunction
            {
                SlotId = slotId,
                Period = period
            };

            try
            {
                var result = gethNode.Call<CanMarkProofAsMissingFunction, string>(Deployment.MarketplaceAddress, func);

                return true;
            }
            catch (SmartContractCustomErrorRevertException)
            {
                return false;
            }
            catch (AggregateException e)
            {
                if (e.InnerExceptions.Count == 1 &&
                    e.InnerExceptions[0] is SmartContractCustomErrorRevertException)
                {
                    return false;
                }
                throw;
            }
        }

        private bool IsProofRequired(byte[] slotId)
        {
            var func = new IsProofRequiredFunction
            {
                Id = slotId
            };
            var result = gethNode.Call<IsProofRequiredFunction, IsProofRequiredOutputDTO>(Deployment.MarketplaceAddress, func);
            return result.ReturnValue1;
        }

        private bool WillProofBeRequired(byte[] slotId)
        {
            var func = new WillProofBeRequiredFunction
            {
                Id = slotId
            };
            var result = gethNode.Call<WillProofBeRequiredFunction, WillProofBeRequiredOutputDTO>(Deployment.MarketplaceAddress, func);
            return result.ReturnValue1;
        }

        private ContractInteractions StartInteraction()
        {
            return new ContractInteractions(log, gethNode);
        }
    }
}
