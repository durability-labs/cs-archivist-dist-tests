using BlockchainUtils;
using ArchivistContractsPlugin.Marketplace;
using GethPlugin;
using Logging;
using Nethereum.Hex.HexConvertors.Extensions;
using System.Numerics;
using Utils;

namespace ArchivistContractsPlugin.ChainMonitor
{
    public interface IChainStateChangeHandler
    {
        void OnNewRequest(RequestEvent requestEvent);
        void OnRequestFinished(RequestEvent requestEvent);
        void OnRequestFulfilled(RequestEvent requestEvent);
        void OnRequestCancelled(RequestEvent requestEvent);
        void OnRequestFailed(RequestEvent requestEvent);
        void OnSlotFilled(RequestEvent requestEvent, EthAddress host, BigInteger slotIndex, bool isRepair);
        void OnSlotFreed(RequestEvent requestEvent, BigInteger slotIndex);
        void OnSlotReservationsFull(RequestEvent requestEvent, BigInteger slotIndex);
        void OnProofSubmitted(BlockTimeEntry block, string id);
        void OnError(string msg);
    }

    public class RequestEvent
    {
        public RequestEvent(BlockTimeEntry block, IChainStateRequest request)
        {
            Block = block;
            Request = request;
        }

        public BlockTimeEntry Block { get; }
        public IChainStateRequest Request { get; }
    }

    public class ChainState
    {
        private readonly List<ChainStateRequest> requests = new List<ChainStateRequest>();
        private readonly ILog log;
        private readonly IGethNode geth;
        private readonly IArchivistContracts contracts;
        private readonly IChainStateChangeHandler handler;
        private readonly bool doProofPeriodMonitoring;

        public ChainState(ILog log, IGethNode geth, IArchivistContracts contracts, IChainStateChangeHandler changeHandler, DateTime startUtc, bool doProofPeriodMonitoring, IPeriodMonitorEventHandler periodEventHandler)
        {
            this.log = new LogPrefixer(log, "(ChainState) ");
            this.geth = geth;
            this.contracts = contracts;
            handler = changeHandler;
            this.doProofPeriodMonitoring = doProofPeriodMonitoring;
            TotalSpan = new TimeRange(startUtc, startUtc);
            PeriodMonitor = new PeriodMonitor(log, contracts, geth, periodEventHandler);

            Initialize(startUtc);
        }

        public TimeRange TotalSpan { get; private set; }
        public IChainStateRequest[] Requests => requests.ToArray();
        public PeriodMonitor PeriodMonitor { get; }
        public BlockTimeEntry CurrentBlock { get; private set; } = null!;

        public bool TryAddRequest(byte[] requestId)
        {
            return FindRequest(requestId) != null;
        }

        public int Update()
        {
            return Update(DateTime.UtcNow);
        }

        public int Update(DateTime toUtc)
        {
            var name = $"{nameof(Update)}({Time.FormatTimestamp(toUtc)})";
            return Stopwatch.Measure(log, name, () => UpdateInternal(toUtc), true).Value;
        }

        private int UpdateInternal(DateTime toUtc)
        {
            var entry = geth.GetHighestBlockBeforeUtc(toUtc);
            if (entry == null) throw new Exception("Unable to find block for update utc: " + Time.FormatTimestamp(toUtc));
            var span = new BlockInterval(new TimeRange(CurrentBlock.Utc, entry.Utc), CurrentBlock.BlockNumber + 1, entry.BlockNumber);
            var events = ChainEvents.FromBlockInterval(contracts, span);
            Apply(events);

            TotalSpan = new TimeRange(TotalSpan.From, entry.Utc);
            CurrentBlock = entry;
            return events.All.Length;
        }

        private void Initialize(DateTime startingUtc)
        {
            var entry = geth.GetHighestBlockBeforeUtc(startingUtc);
            if (entry == null)
            {
                log.Error("Unable to find block for starting utc: " + Time.FormatTimestamp(startingUtc));
                log.Error("Going with block 1 instead...");
                entry = geth.GetBlockForNumber(1);
                if (entry == null) throw new Exception($"Unable to initialize chainstate. Unable to get block at starting UTC {Time.FormatTimestamp(startingUtc)} AND unable to initialize to block number 1.");
            }

            TotalSpan = new TimeRange(TotalSpan.From, entry.Utc);
            CurrentBlock = entry;

            log.Log("Initialized to " + CurrentBlock);
        }

        private void Apply(ChainEvents events)
        {
            if (events.BlockInterval.TimeRange.From < TotalSpan.From)
            {
                var msg = $"Attempt to update ChainState with set of events from before its current record. " +
                    $"TotalSpan: {TotalSpan}" +
                    $"Blocks: {events.BlockInterval} " +
                    $"Times: {events.BlockInterval.TimeRange}";
                handler.OnError(msg);
                throw new Exception(msg);
            }

            log.Debug($"ChainState updating: {events.BlockInterval} = {events.All.Length} events.");

            // Run through each block and apply the events to the state in order.
            // Even when there are no events in the list, still run through each block:
            // There might be time-based OR period-based actions that need to happen at each step.
            var blockTimeGetter = new BlockTimeGetter(events.BlockInterval);
            for (var b = events.BlockInterval.From; b <= events.BlockInterval.To; b++)
            {
                var entry = blockTimeGetter.Get(b);
                var blockEvents = events.All.Where(e => e.Block.BlockNumber == b).ToArray();
                ApplyEvents(entry, blockEvents);
                UpdatePeriodMonitor(entry);
            }
        }

        private void UpdatePeriodMonitor(BlockTimeEntry block)
        {
            if (!doProofPeriodMonitoring) return;
            PeriodMonitor.Update(block, requests.ToArray());
        }

        private void ApplyEvents(BlockTimeEntry entry, IHasBlock[] blockEvents)
        {
            foreach (var e in blockEvents)
            {
                dynamic d = e;
                ApplyEvent(d);
            }

            ApplyTimeImplicitEvents(entry);
        }

        private void ApplyEvent(StorageRequestedEventDTO @event)
        {
            var r = FindRequest(@event);
            if (r == null) throw new Exception("ChainState is inconsistent. Failed to find request after receiving creation event.");
          
            handler.OnNewRequest(new RequestEvent(@event.Block, r));
        }

        private void ApplyEvent(RequestFulfilledEventDTO @event)
        {
            var r = FindRequest(@event);
            if (r == null) return;
            r.UpdateStateFromEvent(@event, RequestState.Started);
            handler.OnRequestFulfilled(new RequestEvent(@event.Block, r));
        }

        private void ApplyEvent(RequestCancelledEventDTO @event)
        {
            var r = FindRequest(@event);
            if (r == null) return;
            r.UpdateStateFromEvent(@event, RequestState.Cancelled);
            handler.OnRequestCancelled(new RequestEvent(@event.Block, r));
        }

        private void ApplyEvent(RequestFailedEventDTO @event)
        {
            var r = FindRequest(@event);
            if (r == null) return;
            r.UpdateStateFromEvent(@event, RequestState.Failed);
            handler.OnRequestFailed(new RequestEvent(@event.Block, r));
        }

        private void ApplyEvent(SlotFilledEventDTO @event)
        {
            var r = FindRequest(@event);
            if (r == null) return;
            var slotIndex = (int)@event.SlotIndex;
            var isRepair = !r.Hosts.IsFilled(slotIndex) && r.Hosts.WasPreviouslyFilled(slotIndex);
            r.Log($"{@event.Block} SlotFilled (host:'{@event.Host}', slotIndex:{@event.SlotIndex}, isRepair:{isRepair})");
            r.Hosts.HostFillsSlot(@event.Host, slotIndex);
            handler.OnSlotFilled(new RequestEvent(@event.Block, r), @event.Host, @event.SlotIndex, isRepair);
        }

        private void ApplyEvent(SlotFreedEventDTO @event)
        {
            var r = FindRequest(@event);
            if (r == null) return;
            var host = r.Hosts.GetHost((int)@event.SlotIndex);
            r.Log($"{@event.Block} SlotFreed (slotIndex:{@event.SlotIndex}, previousHost:{host.AsStr()} )");
            r.Hosts.SlotFreed((int)@event.SlotIndex);
            handler.OnSlotFreed(new RequestEvent(@event.Block, r), @event.SlotIndex);
        }

        private void ApplyEvent(SlotReservationsFullEventDTO @event)
        {
            var r = FindRequest(@event);
            if (r == null) return;
            r.Log($"{@event.Block} SlotReservationsFull (slotIndex:{@event.SlotIndex})");
            handler.OnSlotReservationsFull(new RequestEvent(@event.Block, r), @event.SlotIndex);
        }

        private void ApplyEvent(ProofSubmittedEventDTO @event)
        {
            var id = Base58.Encode(@event.Id);

            var proofOrigin = SearchForProofOrigin(id);

            log.Log($"{@event.Block} Proof submitted (id:{id} {proofOrigin})");
            handler.OnProofSubmitted(@event.Block, id);
        }

        private string SearchForProofOrigin(string slotId)
        {
            foreach (var r in requests)
            {
                for (decimal slotIndex = 0; slotIndex < r.Ask.Slots; slotIndex++)
                {
                    var thisSlotId = contracts.GetSlotId(r.RequestId, slotIndex);
                    var id = Base58.Encode(thisSlotId);

                    if (id.ToLowerInvariant() == slotId.ToLowerInvariant())
                    {
                        return $"({r.RequestId.ToHex()} slotIndex:{slotIndex})";
                    }
                }
            }
            return "(Could not identify proof requestId + slot)";
        }

        private void ApplyTimeImplicitEvents(BlockTimeEntry entry)
        {
            foreach (var r in requests)
            {
                if (r.State == RequestState.Started
                    && r.FinishedUtc < entry.Utc)
                {
                    r.UpdateStateFromTime(
                        matchingBlock: entry,
                        eventName: "RequestFinished",
                        newState: RequestState.Finished);
                    handler.OnRequestFinished(new RequestEvent(entry, r));
                }
            }
        }

        private ChainStateRequest? FindRequest(IHasRequestId hasRequestId)
        {
            return FindRequest(hasRequestId.RequestId);
        }

        private ChainStateRequest? FindRequest(byte[] requestId)
        {
            var r = requests.SingleOrDefault(r => ByteArrayUtils.Equal(r.RequestId, requestId));
            if (r != null) return r;

            try
            {
                var request = contracts.GetRequest(requestId);
                if (request == null) return null;
                var state = contracts.GetRequestState(requestId);
                if (state == null) return null;
                var newRequest = new ChainStateRequest(log, requestId, request, state.Value);
                requests.Add(newRequest);
                return newRequest;
            }
            catch (Exception ex)
            {
                var msg = $"Failed to get request with id '{requestId.ToHex()}' from chain: {ex}";
                log.Error(msg);
                handler.OnError(msg);
                return null;
            }
        }

        public class BlockTimeGetter
        {
            private readonly BlockInterval interval;
            private readonly TimeSpan spanPerBlock;

            public BlockTimeGetter(BlockInterval interval)
            {
                var numBlocks = interval.NumberOfBlocks;
                var span = interval.TimeRange.Duration;
                spanPerBlock = span / numBlocks;
                this.interval = interval;
            }

            public BlockTimeEntry Get(ulong blockNumber)
            {
                // It's too time-consuming to get the blockentry for every block in the range.
                // Instead, we make one up!
                // We do this by assuming the blocks in the range are evenly spaced in time.
                // While that's not necessarily true, it's a good enough approximation for our purposes.

                if (blockNumber > interval.To) throw new Exception($"Block number {blockNumber} is out of range {interval} (over)");
                if (blockNumber < interval.From) throw new Exception($"Block number {blockNumber} is out of range {interval} (under)");

                var count = blockNumber - interval.From;
                var blockUtc = interval.TimeRange.From + (count * spanPerBlock);
                var entry = new BlockTimeEntry(blockNumber, blockUtc);
                if (blockUtc > interval.TimeRange.To)
                {
                    throw new InvalidOperationException(
                        $"BlockRange: {interval} " +
                        $"found spanPerBlock: '{Time.FormatDuration(spanPerBlock)}' - " +
                        $"at block {blockNumber} found blockUtc at {Time.FormatTimestamp(blockUtc)} which is past end of time range.");
                }
                return entry;
            }
        }
    }
}
