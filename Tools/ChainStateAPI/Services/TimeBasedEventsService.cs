using ChainStateAPI.Database;
using Utils;

namespace ChainStateAPI.Services
{
    public interface ITimeBasedEventsService
    {
        TimeEvents GetTimeBasedEvents(TimeRange timeRange);
    }

    public class TimeBasedEventsService : ITimeBasedEventsService
    {
        private readonly IDatabaseService databaseService;
        private readonly IDeploymentService deploymentService;

        public TimeBasedEventsService(IDatabaseService databaseService, IDeploymentService deploymentService)
        {
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
            var contracts = databaseService.Query<DbContractsContext, StorageContract[]>(context =>
            {
                return context.StorageContracts.Where(c => timeRange.Includes(c.FinishedUtc)).ToArray();
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
            var contracts = databaseService.Query<DbContractsContext, StorageContract[]>(context =>
            {
                return context.StorageContracts.Where(c => timeRange.Includes(c.ExpiryUtc)).ToArray();
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
    }
}
