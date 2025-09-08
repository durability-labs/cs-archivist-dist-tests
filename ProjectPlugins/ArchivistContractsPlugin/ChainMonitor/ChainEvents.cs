using ArchivistContractsPlugin.Marketplace;
using Utils;

namespace ArchivistContractsPlugin.ChainMonitor
{
    public class ChainEvents
    {
        private ChainEvents(
            BlockInterval blockInterval,
            StorageRequestedEventDTO[] requests,
            RequestFulfilledEventDTO[] fulfilled,
            RequestCancelledEventDTO[] cancelled,
            RequestFailedEventDTO[] failed,
            SlotFilledEventDTO[] slotFilled,
            SlotFreedEventDTO[] slotFreed,
            SlotReservationsFullEventDTO[] slotReservationsFull,
            ProofSubmittedEventDTO[] proofSubmitted
            )
        {
            BlockInterval = blockInterval;
            Requests = requests;
            Fulfilled = fulfilled;
            Cancelled = cancelled;
            Failed = failed;
            SlotFilled = slotFilled;
            SlotFreed = slotFreed;
            SlotReservationsFull = slotReservationsFull;
            ProofSubmitted = proofSubmitted;
            All = ConcatAll<IHasBlock>(requests, fulfilled, cancelled, failed, slotFilled, SlotFreed, SlotReservationsFull, ProofSubmitted);
        }

        public BlockInterval BlockInterval { get; }
        public StorageRequestedEventDTO[] Requests { get; }
        public RequestFulfilledEventDTO[] Fulfilled { get; }
        public RequestCancelledEventDTO[] Cancelled { get; }
        public RequestFailedEventDTO[] Failed { get; }
        public SlotFilledEventDTO[] SlotFilled { get; }
        public SlotFreedEventDTO[] SlotFreed { get; }
        public SlotReservationsFullEventDTO[] SlotReservationsFull { get; }
        public ProofSubmittedEventDTO[] ProofSubmitted { get; }
        public IHasBlock[] All { get; }

        public static ChainEvents FromBlockInterval(IArchivistContracts contracts, BlockInterval blockInterval)
        {
            return FromContractEvents(contracts.GetEvents(blockInterval));
        }

        public static ChainEvents FromTimeRange(IArchivistContracts contracts, TimeRange timeRange)
        {
            return FromContractEvents(contracts.GetEvents(timeRange));
        }

        public static ChainEvents FromContractEvents(IArchivistContractsEvents events)
        {
            return new ChainEvents(
                events.BlockInterval,
                events.GetStorageRequestedEvents(),
                events.GetRequestFulfilledEvents(),
                events.GetRequestCancelledEvents(),
                events.GetRequestFailedEvents(),
                events.GetSlotFilledEvents(),
                events.GetSlotFreedEvents(),
                events.GetSlotReservationsFullEvents(),
                events.GetProofSubmittedEvents()
            );
        }

        private T[] ConcatAll<T>(params T[][] arrays)
        {
            var result = Array.Empty<T>();
            foreach (var array in arrays)
            {
                result = result.Concat(array).ToArray();
            }
            return result;
        }
    }
}
