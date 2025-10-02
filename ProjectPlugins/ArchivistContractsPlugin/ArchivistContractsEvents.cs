using BlockchainUtils;
using ArchivistContractsPlugin.Marketplace;
using GethPlugin;
using Logging;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Utils;
using NethereumWorkflow;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace ArchivistContractsPlugin
{
    public interface IArchivistContractsEvents
    {
        BlockInterval BlockInterval { get; }
        IContractEventsCollector[] GetEvents(params IContractEventsCollector[] collectors);
        TEvent[] GetEvents<TEvent>() where TEvent : IEventDTO, new();
        void GetReserveSlotCalls(Action<ReserveSlotFunction> onFunction);
    }

    public interface IContractEventsCollector
    {
        IEventsCollector Collector { get; }
        void Map(ILog log, IGethNode gethNode);
    }

    public class ContractEventsCollector<TEvent> : IContractEventsCollector where TEvent : IEventDTO, new()
    {
        private readonly EventsCollector<TEvent> collector;

        public ContractEventsCollector()
        {
            collector = new EventsCollector<TEvent>();
        }

        public IEventsCollector Collector => collector;
        public List<TEvent> Events { get; } = new();

        public void Map(ILog log, IGethNode gethNode)
        {
            foreach (var e in collector.Events)
            {
                SetBlockOnEvent(gethNode, e);

                if (e.Event is SlotFilledEventDTO slotFilled)
                {
                    slotFilled.Host = GetEthAddressFromTransaction(gethNode, e.Log.TransactionHash);
                }

                Events.Add(e.Event);
            }

            if (Events.Count > 0) log.Debug($"Collector<{typeof(TEvent).Name}>: {Events.Count} events");
        }

        private void SetBlockOnEvent(IGethNode gethNode, EventLog<TEvent> e)
        {
            if (e.Event is IHasBlock hasBlock)
            {
                hasBlock.Block = GetBlock(gethNode, e.Log.BlockNumber.ToUlong());
            }
        }

        private BlockTimeEntry GetBlock(IGethNode gethNode, ulong number)
        {
            var entry = gethNode.GetBlockForNumber(number);
            if (entry == null) throw new Exception("Failed to find block by number: " + number);
            return entry;
        }

        private EthAddress GetEthAddressFromTransaction(IGethNode gethNode, string transactionHash)
        {
            var transaction = gethNode.GetTransaction(transactionHash);
            return new EthAddress(transaction.From);
        }
    }

    public class ArchivistContractsEvents : IArchivistContractsEvents
    {
        private readonly ILog log;
        private readonly IGethNode gethNode;
        private readonly ArchivistContractsDeployment deployment;

        public ArchivistContractsEvents(ILog log, IGethNode gethNode, ArchivistContractsDeployment deployment, BlockInterval blockInterval)
        {
            this.log = log;
            this.gethNode = gethNode;
            this.deployment = deployment;
            BlockInterval = blockInterval;
        }
        
        public BlockInterval BlockInterval { get; }

        public IContractEventsCollector[] GetEvents(params IContractEventsCollector[] collectors)
        {
            gethNode.GetEvents(deployment.MarketplaceAddress, BlockInterval, collectors.Select(c => c.Collector).ToArray());
            foreach (var c in collectors) c.Map(log, gethNode);
            return collectors;
        }

        public TEvent[] GetEvents<TEvent>() where TEvent : IEventDTO, new()
        {
            var collector = new ContractEventsCollector<TEvent>();
            GetEvents(collector);
            return collector.Events.ToArray();
        }

        public void GetReserveSlotCalls(Action<ReserveSlotFunction> onFunction)
        {
            var count = 0;
            gethNode.IterateFunctionCalls<ReserveSlotFunction>(BlockInterval, (b, fn) =>
            {
                if (b == null) throw new Exception("Block not provided for event. " + nameof(ReserveSlotFunction));
                fn.Block = b;
                onFunction(fn);
                count++;
            });
            log.Debug($"{BlockInterval} {nameof(ReserveSlotFunction)} => {count}");
        }
    }
}
