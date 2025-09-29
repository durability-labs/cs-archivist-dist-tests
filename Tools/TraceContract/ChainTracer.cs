using ArchivistContractsPlugin;
using ArchivistContractsPlugin.ChainMonitor;
using ArchivistContractsPlugin.Marketplace;
using BlockchainUtils;
using GethPlugin;
using Logging;
using Nethereum.Hex.HexConvertors.Extensions;
using Utils;

namespace TraceContract
{
    public class ChainTracer
    {
        private readonly ILog log;
        private readonly IGethNode geth;
        private readonly IArchivistContracts contracts;
        private readonly Input input;
        private readonly Output output;

        public ChainTracer(ILog log, IGethNode geth, IArchivistContracts contracts, Input input, Output output)
        {
            this.log = log;
            this.geth = geth;
            this.contracts = contracts;
            this.input = input;
            this.output = output;
        }

        public TimeRange TraceChainTimeline()
        {
            log.Log("Querying blockchain...");
            var request = GetRequest();
            if (request == null) throw new Exception("Failed to find the purchase in the last week of transactions.");

            var creationEvent = FindRequestCreationEvent();

            log.Log($"Request started at {creationEvent.Block.Utc}");
            var contractEnd = RunToContractEnd(creationEvent);

            log.Log($"Request timeline: {creationEvent.Block} -> {contractEnd}");

            // For this timeline, we log all the calls to reserve-slot.
            var blockRange = new BlockInterval(
                new TimeRange(creationEvent.Block.Utc, contractEnd.Utc),
                creationEvent.Block.BlockNumber,
                contractEnd.BlockNumber);

            var events = contracts.GetEvents(blockRange);

            events.GetReserveSlotCalls(call =>
            {
                if (IsThisRequest(call.RequestId))
                {
                    output.LogReserveSlotCall(call);
                    log.Log("Found reserve-slot call for slotIndex " + call.SlotIndex);
                }
            });

            log.Log("Writing blockchain output...");
            output.WriteContractEvents();

            return blockRange.TimeRange;
        }

        private BlockTimeEntry RunToContractEnd(StorageRequestedEventDTO request)
        {
            var utc = request.Block.Utc.AddMinutes(-1.0);
            var tracker = new ChainRequestTracker(output, input.PurchaseId);
            var ignoreLog = new NullLog();
            var chainState = new ChainState(ignoreLog, geth, contracts, tracker, utc, false, new DoNothingPeriodMonitorEventHandler());

            while (!tracker.IsFinished)
            {
                utc += TimeSpan.FromHours(1.0);
                if (utc > DateTime.UtcNow)
                {
                    log.Log("Caught up to present moment without finding contract end.");
                    return geth.GetBlockForUtc(DateTime.UtcNow)!;
                }

                log.Log($"Querying up to {utc}");
                chainState.Update(utc);
            }

            return tracker.FinishBlock!;
        }

        private bool IsThisRequest(byte[] requestId)
        {
            return requestId.ToHex().ToLowerInvariant() == input.PurchaseId.ToLowerInvariant();
        }

        private Request? GetRequest()
        {
            return contracts.GetRequest(input.RequestId);
        }

        public StorageRequestedEventDTO FindRequestCreationEvent()
        {
            ulong blocksPerLoop = 3600;
            var end = geth.GetBlockForUtc(DateTime.UtcNow)!;
            var start = geth.GetBlockForNumber(end.BlockNumber - blocksPerLoop)!;
            var limit = geth.GetBlockForUtc(DateTime.UtcNow - TimeSpan.FromDays(30))!;
            var range = new BlockInterval(new TimeRange(start.Utc, end.Utc), start.BlockNumber, end.BlockNumber);

            while (range.From > limit.BlockNumber)
            {
                var events = contracts.GetEvents(range);
                var requests = events.GetEvents<StorageRequestedEventDTO>();
                foreach (var r in requests)
                {
                    if (r.RequestId.ToHex() == input.RequestId.ToHex()) return r;
                }

                end = start;
                start = geth.GetBlockForNumber(end.BlockNumber - blocksPerLoop)!;
                range = new BlockInterval(new TimeRange(start.Utc, end.Utc), start.BlockNumber, end.BlockNumber);
            }

            throw new Exception("Unable to find storage request creation event on-chain after (limit) 30 days");
        }
    }
}
