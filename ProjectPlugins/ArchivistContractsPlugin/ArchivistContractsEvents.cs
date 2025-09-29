using BlockchainUtils;
using ArchivistContractsPlugin.Marketplace;
using GethPlugin;
using Logging;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Utils;

namespace ArchivistContractsPlugin
{
    public interface IArchivistContractsEvents
    {
        BlockInterval BlockInterval { get; }
        StorageRequestedEventDTO[] GetStorageRequestedEvents();
        RequestFulfilledEventDTO[] GetRequestFulfilledEvents();
        RequestCancelledEventDTO[] GetRequestCancelledEvents();
        RequestFailedEventDTO[] GetRequestFailedEvents();
        SlotFilledEventDTO[] GetSlotFilledEvents();
        SlotFreedEventDTO[] GetSlotFreedEvents();
        SlotReservationsFullEventDTO[] GetSlotReservationsFullEvents();
        ProofSubmittedEventDTO[] GetProofSubmittedEvents();
        void GetReserveSlotCalls(Action<ReserveSlotFunction> onFunction);
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

        public StorageRequestedEventDTO[] GetStorageRequestedEvents()
        {
            var events = gethNode.GetEvents<StorageRequestedEventDTO>(deployment.MarketplaceAddress, BlockInterval);
            return DebugLog(events.Select(SetBlockOnEvent).ToArray());
        }

        public RequestFulfilledEventDTO[] GetRequestFulfilledEvents()
        {
            var events = gethNode.GetEvents<RequestFulfilledEventDTO>(deployment.MarketplaceAddress, BlockInterval);
            return DebugLog(events.Select(SetBlockOnEvent).ToArray());
        }

        public RequestCancelledEventDTO[] GetRequestCancelledEvents()
        {
            var events = gethNode.GetEvents<RequestCancelledEventDTO>(deployment.MarketplaceAddress, BlockInterval);
            return DebugLog(events.Select(SetBlockOnEvent).ToArray());
        }

        public RequestFailedEventDTO[] GetRequestFailedEvents()
        {
            var events = gethNode.GetEvents<RequestFailedEventDTO>(deployment.MarketplaceAddress, BlockInterval);
            return DebugLog(events.Select(SetBlockOnEvent).ToArray());
        }

        public SlotFilledEventDTO[] GetSlotFilledEvents()
        {
            var events = gethNode.GetEvents<SlotFilledEventDTO>(deployment.MarketplaceAddress, BlockInterval);
            return DebugLog(events.Select(e =>
            {
                var result = e.Event;
                result.Block = GetBlock(e.Log.BlockNumber.ToUlong());
                result.Host = GetEthAddressFromTransaction(e.Log.TransactionHash);
                return result;
            }).ToArray());
        }

        public SlotFreedEventDTO[] GetSlotFreedEvents()
        {
            var events = gethNode.GetEvents<SlotFreedEventDTO>(deployment.MarketplaceAddress, BlockInterval);
            return DebugLog(events.Select(SetBlockOnEvent).ToArray());
        }

        public SlotReservationsFullEventDTO[] GetSlotReservationsFullEvents()
        {
            var events = gethNode.GetEvents<SlotReservationsFullEventDTO>(deployment.MarketplaceAddress, BlockInterval);
            return DebugLog(events.Select(SetBlockOnEvent).ToArray());
        }

        public ProofSubmittedEventDTO[] GetProofSubmittedEvents()
        {
            var events = gethNode.GetEvents<ProofSubmittedEventDTO>(deployment.MarketplaceAddress, BlockInterval);
            return DebugLog(events.Select(SetBlockOnEvent).ToArray());
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

        private T SetBlockOnEvent<T>(EventLog<T> e) where T : IHasBlock
        {
            var result = e.Event;
            result.Block = GetBlock(e.Log.BlockNumber.ToUlong());
            return result;
        }

        private BlockTimeEntry GetBlock(ulong number)
        {
            var entry = gethNode.GetBlockForNumber(number);
            if (entry == null) throw new Exception("Failed to find block by number: " + number);
            return entry;
        }

        private EthAddress GetEthAddressFromTransaction(string transactionHash)
        {
            var transaction = gethNode.GetTransaction(transactionHash);
            return new EthAddress(transaction.From);
        }

        private T[] DebugLog<T>(T[] events)
        {
            log.Debug($"{BlockInterval} {typeof(T).Name} => {events.Length}");
            return events;
        }
    }
}
