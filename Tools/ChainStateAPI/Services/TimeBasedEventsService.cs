using ChainStateAPI.Database;
using Logging;
using Utils;

namespace ChainStateAPI.Services
{
    public interface ITimeBasedEventsService
    {
        TimeEvents GetTimeBasedEvents(TimeRange timeRange);
    }

    public class TimeBasedEventsService : ITimeBasedEventsService
    {
        private readonly ILog log;
        private readonly IDatabaseService databaseService;
        private readonly IDeploymentService deploymentService;

        public TimeBasedEventsService(ILog log, IDatabaseService databaseService, IDeploymentService deploymentService)
        {
            this.log = log;
            this.databaseService = databaseService;
            this.deploymentService = deploymentService;
        }

        public TimeEvents GetTimeBasedEvents(TimeRange timeRange)
        {
            return new TimeEvents
            {
                CancelledEvents = QueryCancelledEvents(timeRange),
                FinishedEvents = QueryFinishedEvents(timeRange)
            };
        }

        private ContractFinished[] QueryFinishedEvents(TimeRange timeRange)
        {
            log.Debug("Querying for finished-events...");
            var contracts = databaseService.Query<DbContractsContext, StorageContract[]>(context =>
            {
                return context.StorageContracts.Where(c =>
                    timeRange.From <= c.FinishedUtc && c.FinishedUtc < timeRange.To
                ).ToArray();
            });

            return contracts.Select(c => new ContractFinished
            {
                BlockNumber = deploymentService.RpcNode.GetHighestBlockBeforeUtc(c.FinishedUtc).BlockNumber,
                Utc = c.FinishedUtc,
                RequestId = c.RequestId,
            }).ToArray();
        }

        private ContractCancelled[] QueryCancelledEvents(TimeRange timeRange)
        {
            log.Debug("Querying for cancelled-events...");
            var contracts = databaseService.Query<DbContractsContext, StorageContract[]>(context =>
            {
                return context.StorageContracts.Where(c =>
                    timeRange.From <= c.ExpiryUtc && c.ExpiryUtc < timeRange.To
                ).ToArray();
            });

            return contracts.Select(c => new ContractCancelled
            {
                BlockNumber = deploymentService.RpcNode.GetHighestBlockBeforeUtc(c.FinishedUtc).BlockNumber,
                Utc = c.ExpiryUtc,
                RequestId = c.RequestId,
            }).ToArray();
        }
    }

    public class TimeEvents
    {
        public ContractCancelled[] CancelledEvents { get; set; } = Array.Empty<ContractCancelled>();
        public ContractFinished[] FinishedEvents { get; set; } = Array.Empty<ContractFinished>();

        public bool IsEmpty()
        {
            return CancelledEvents.Length == 0 && FinishedEvents.Length == 0;
        }
    }
}
