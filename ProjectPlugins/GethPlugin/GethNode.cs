using BlockchainUtils;
using Core;
using KubernetesWorkflow.Types;
using Logging;
using Nethereum.Contracts;
using Nethereum.RPC.Eth.DTOs;
using NethereumWorkflow;
using Utils;

namespace GethPlugin
{
    public interface IGethNode : IHasContainer
    {
        GethDeployment StartResult { get; }
        EthAddress CurrentAddress { get; }

        Ether GetEthBalance();
        Ether GetEthBalance(IHasEthAddress address);
        Ether GetEthBalance(EthAddress address);
        string SendEth(IHasEthAddress account, Ether eth);
        string SendEth(EthAddress account, Ether eth);
        TResult Call<TFunction, TResult>(ContractAddress contractAddress, TFunction function) where TFunction : FunctionMessage, new();
        TResult Call<TFunction, TResult>(ContractAddress contractAddress, TFunction function, ulong blockNumber) where TFunction : FunctionMessage, new();
        void Call<TFunction>(ContractAddress contractAddress, TFunction function) where TFunction : FunctionMessage, new();
        void Call<TFunction>(ContractAddress contractAddress, TFunction function, ulong blockNumber) where TFunction : FunctionMessage, new();
        string SendTransaction<TFunction>(ContractAddress contractAddress, TFunction function) where TFunction : FunctionMessage, new();
        Transaction GetTransaction(string transactionHash);
        decimal? GetSyncedBlockNumber();
        bool IsContractAvailable(string abi, ContractAddress contractAddress);
        GethBootstrapNode GetBootstrapRecord();
        IEventsCollector[] GetEvents(ContractAddress address, BlockInterval blockRange, params IEventsCollector[] collectors);
        IFunctionCallCollector[] GetFunctionCalls(ContractAddress address, BlockInterval blockRange, params IFunctionCallCollector[] collectors);
        BlockTimeEntry? GetBlockForNumber(ulong number);
        BlockTimeEntry? GetBlockForUtc(DateTime utc);
        IGethNode WithDifferentAccount(EthAccount account);
    }

    public class DeploymentGethNode : BaseGethNode, IGethNode
    {
        private readonly ILog log;
        private readonly BlockCache blockCache;

        public DeploymentGethNode(ILog log, BlockCache blockCache, GethDeployment startResult)
        {
            this.log = log;
            this.blockCache = blockCache;
            StartResult = startResult;
            CurrentAddress = new EthAddress(startResult.Account.Account);
        }

        public GethDeployment StartResult { get; }
        public RunningContainer Container => StartResult.Container;
        public EthAddress CurrentAddress { get; }

        public GethBootstrapNode GetBootstrapRecord()
        {
            var address = StartResult.Container.GetInternalAddress(GethContainerRecipe.ListenPortTag);

            return new GethBootstrapNode(
                publicKey: StartResult.PubKey,
                ipAddress: address.Host.Replace("http://", ""),
                port: address.Port
            );
        }

        protected override NethereumInteraction StartInteraction()
        {
            var address = StartResult.Container.GetAddress(GethContainerRecipe.HttpPortTag);
            var account = StartResult.Account;

            var creator = new NethereumInteractionCreator(log, blockCache, $"{address.Host}:{address.Port}", account.PrivateKey);
            return creator.CreateWorkflow();
        }

        public IGethNode WithDifferentAccount(EthAccount account)
        {
            return new DeploymentGethNode(log, blockCache,
                new GethDeployment(
                    StartResult.Pod,
                    StartResult.DiscoveryPort,
                    StartResult.HttpPort,
                    StartResult.WsPort,
                    new GethAccount(
                        account.EthAddress.Address,
                        account.PrivateKey
                    ),
                    account.PrivateKey));
        }
    }

    public class CustomGethNode : BaseGethNode, IGethNode
    {
        private readonly ILog log;
        private readonly BlockCache blockCache;
        private readonly string rpcUrl;
        private readonly string privateKey;

        public GethDeployment StartResult => throw new NotImplementedException();
        public RunningContainer Container => throw new NotImplementedException();
        public EthAddress CurrentAddress { get; }

        public CustomGethNode(ILog log, BlockCache blockCache, string rpcUrl, string privateKey)
        {
            this.log = log;
            this.blockCache = blockCache;
            this.rpcUrl = rpcUrl;
            this.privateKey = privateKey;

            var creator = new NethereumInteractionCreator(log, blockCache, rpcUrl, privateKey);
            CurrentAddress = creator.GetEthAddress();
        }

        public GethBootstrapNode GetBootstrapRecord()
        {
            throw new NotImplementedException();
        }

        public IGethNode WithDifferentAccount(EthAccount account)
        {
            return new CustomGethNode(log, blockCache, rpcUrl, account.PrivateKey);
        }

        protected override NethereumInteraction StartInteraction()
        {
            var creator = new NethereumInteractionCreator(log, blockCache, rpcUrl, privateKey);
            return creator.CreateWorkflow();
        }
    }

    public abstract class BaseGethNode
    {
        public Ether GetEthBalance()
        {
            return StartInteraction().GetEthBalance().Wei();
        }

        public Ether GetEthBalance(IHasEthAddress owner)
        {
            return GetEthBalance(owner.EthAddress);
        }

        public Ether GetEthBalance(EthAddress address)
        {
            return StartInteraction().GetEthBalance(address.Address).Wei();
        }

        public string SendEth(IHasEthAddress owner, Ether eth)
        {
            return SendEth(owner.EthAddress, eth);
        }

        public string SendEth(EthAddress account, Ether eth)
        {
            return StartInteraction().SendEth(account.Address, eth);
        }

        public TResult Call<TFunction, TResult>(ContractAddress contractAddress, TFunction function) where TFunction : FunctionMessage, new()
        {
            return StartInteraction().Call<TFunction, TResult>(contractAddress, function);
        }

        public TResult Call<TFunction, TResult>(ContractAddress contractAddress, TFunction function, ulong blockNumber) where TFunction : FunctionMessage, new()
        {
            return StartInteraction().Call<TFunction, TResult>(contractAddress, function, blockNumber);
        }

        public void Call<TFunction>(ContractAddress contractAddress, TFunction function) where TFunction : FunctionMessage, new()
        {
            StartInteraction().Call(contractAddress, function);
        }

        public void Call<TFunction>(ContractAddress contractAddress, TFunction function, ulong blockNumber) where TFunction : FunctionMessage, new()
        {
            StartInteraction().Call(contractAddress, function, blockNumber);
        }

        public string SendTransaction<TFunction>(ContractAddress contractAddress, TFunction function) where TFunction : FunctionMessage, new()
        {
            return StartInteraction().SendTransaction(contractAddress, function);
        }

        public Transaction GetTransaction(string transactionHash)
        {
            return StartInteraction().GetTransaction(transactionHash);
        }

        public decimal? GetSyncedBlockNumber()
        {
            return StartInteraction().GetSyncedBlockNumber();
        }

        public bool IsContractAvailable(string abi, ContractAddress contractAddress)
        {
            return StartInteraction().IsContractAvailable(abi, contractAddress);
        }

        public IEventsCollector[] GetEvents(ContractAddress address, BlockInterval blockRange, params IEventsCollector[] collectors)
        {
            return StartInteraction().GetEvents(address, blockRange, collectors);
        }

        public IFunctionCallCollector[] GetFunctionCalls(ContractAddress address, BlockInterval blockRange, params IFunctionCallCollector[] collectors)
        {
            return StartInteraction().GetFunctionCalls(address, blockRange, collectors);
        }

        public BlockTimeEntry? GetBlockForNumber(ulong number)
        {
            return StartInteraction().GetBlockForNumber(number);
        }

        public BlockTimeEntry? GetBlockForUtc(DateTime utc)
        {
            return StartInteraction().GetBlockForUtc(utc);
        }

        protected abstract NethereumInteraction StartInteraction();
    }
}
