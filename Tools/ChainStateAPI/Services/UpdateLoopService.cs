using ArchivistContractsPlugin;
using ArchivistContractsPlugin.Marketplace;
using BlockchainUtils;
using ChainStateAPI.Database;
using GethPlugin;
using Logging;
using Utils;

namespace ChainStateAPI.Services
{
    public interface IUpdateLoopService
    {
        void Start();
    }

    public class UpdateLoopService : IUpdateLoopService
    {
        private readonly ILog log;
        private readonly IDeploymentService deploymentService;
        private readonly IDatabaseService databaseService;
        private readonly IMapperService mapper;
        private readonly ITimeBasedEventsService timeBasedEvents;
        private BlockTimeEntry lastUpdate = null!;

        public UpdateLoopService(ILog log, IDeploymentService deploymentService, IDatabaseService databaseService, IMapperService mapper, ITimeBasedEventsService timeBasedEvents)
        {
            this.log = log;
            this.deploymentService = deploymentService;
            this.databaseService = databaseService;
            this.mapper = mapper;
            this.timeBasedEvents = timeBasedEvents;
        }

        public void Start()
        {
            var latestBlock = Rpc.GetHighestBlockBeforeUtc(DateTime.UtcNow);
            if (latestBlock == null) throw new Exception($"Unable to fetch latest block.");
            lastUpdate = latestBlock;

            Task.Run(Worker);
        }

        private IGethNode Rpc => deploymentService.RpcNode;

        private void Worker()
        {
            while (true)
            {
                try
                {
                    WorkerStep();
                    Thread.Sleep(3000);
                }
                catch (Exception ex)
                {
                    log.Error($"Exception in update loop: {ex}");
                    return;
                }
            }
        }

        private void WorkerStep()
        {
            var nowBlock = Rpc.GetHighestBlockBeforeUtc(DateTime.UtcNow);
            if (nowBlock.BlockNumber == lastUpdate.BlockNumber) return;

            var span = new BlockInterval(
                new TimeRange(lastUpdate.Utc, nowBlock.Utc),
                from: lastUpdate.BlockNumber, to: nowBlock.BlockNumber
            );
            Debug($"Updating: {span}");

            var events = deploymentService.Contracts.GetEvents(span);
            var storageRequested = new ContractEventsCollector<StorageRequestedEventDTO>();
            var requestFulfilled = new ContractEventsCollector<RequestFulfilledEventDTO>();
            var requestFailed = new ContractEventsCollector<RequestFailedEventDTO>();
            var slotFilled = new ContractEventsCollector<SlotFilledEventDTO>();
            var slotFreed = new ContractEventsCollector<SlotFreedEventDTO>();
            var slotReservationsFull = new ContractEventsCollector<SlotReservationsFullEventDTO>();
            //var proofSubmitted = new ContractEventsCollector<ProofSubmittedEventDTO>();

            events.GetEvents(
                storageRequested,
                requestFulfilled,
                requestFailed,
                slotFilled,
                slotFreed,
                slotReservationsFull
                //proofSubmitted
            );

            // For each event we must make sure that the contract is known and stored before we try to store
            // the events themselves.
            var all = ConcatAll<IHasRequestId>(
                storageRequested.Events,
                requestFulfilled.Events,
                requestFailed.Events,
                slotFilled.Events,
                slotFreed.Events,
                slotReservationsFull.Events);
            EnsureContractsAreStored(all);

            // We compute all time-based events (expired, finished) now and store them as if
            // they were normal chain events.
            var timeEvents = timeBasedEvents.GetTimeBasedEvents(span.TimeRange);

            databaseService.Mutate<DbContractsContext>(context =>
            {
                context.StorageRequestedEvents.AddRange(storageRequested.Events.Select(mapper.Map));
                context.ContractStartedEvents.AddRange(requestFulfilled.Events.Select(mapper.Map));
                context.ContractFailedEvents.AddRange(requestFailed.Events.Select(mapper.Map));
                context.SlotFilledEvents.AddRange(slotFilled.Events.Select(mapper.Map));
                context.SlotFreedEvents.AddRange(slotFreed.Events.Select(mapper.Map));
                context.SlotReservationsFullEvents.AddRange(slotReservationsFull.Events.Select(mapper.Map));

                context.ContractCancelledEvents.AddRange(timeEvents.CancelledEvents);
                context.ContractFinished.AddRange(timeEvents.FinishedEvents);
            });

            lastUpdate = nowBlock;
        }

        private void EnsureContractsAreStored(IHasRequestId[] all)
        {
            var requestIds = all.Select(a => mapper.Map(a.RequestId)).Distinct().ToArray();
            var toFetch = databaseService.Query<DbContractsContext, string[]>(context =>
            {
                return requestIds.Where(id => !context.StorageContracts.Any(s => s.RequestId == id)).ToArray();
            });

            if (toFetch.Length == 0) return;

            Log($"Discovered {toFetch.Length} unknown storage contract IDs. Fetching...");

            var toSave = requestIds.Select(requestId =>
            {
                try
                {
                    return FetchNewContract(requestId);
                }
                catch (Exception ex)
                {
                    log.Error($"Failed to fetch contract from chain for '{requestId}': {ex}");
                    return null;
                }
            }).Where(e => e != null).Cast<StorageContract>().ToArray();

            databaseService.Mutate<DbContractsContext>(context =>
            {
                context.StorageContracts.AddRange(toSave);
            });

            Log($"Saved {toSave.Length} new storage contracts.");
        }

        private StorageContract? FetchNewContract(string requestId)
        {
            var bytes = mapper.Map(requestId);
            var chainRequest = deploymentService.Contracts.GetRequest(bytes);
            if (chainRequest == null)
            {
                Log($"Failed to find: '{requestId}'");
                return null;
            }

            return mapper.Map(requestId, chainRequest);
        }

        private T[] ConcatAll<T>(params IEnumerable<T>[] arrays)
        {
            var result = Array.Empty<T>();
            foreach (var array in arrays)
            {
                result = result.Concat(array).ToArray();
            }
            return result;
        }

        private void Log(string msg)
        {
            log.Log(msg);
        }

        private void Debug(string msg)
        {
            log.Debug(msg);
        }
    }
}
