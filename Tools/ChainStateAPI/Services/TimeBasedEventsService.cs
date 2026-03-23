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
        private readonly IActiveContractsService activeContractsService;

        public TimeBasedEventsService(IActiveContractsService activeContractsService)
        {
            this.activeContractsService = activeContractsService;
        }

        public TimeEvents GetTimeBasedEvents(TimeRange timeRange)
        {
            var result = new TimeEvents();
            foreach (var c in activeContractsService.GetActiveContracts())
            {
                Apply(result, c);
            }

            return result;
        }

        private void Apply(TimeEvents result, StorageContract c)
        {
            throw new NotImplementedException();
        }
    }

    public class TimeEvents
    {
        public ContractCancelled[] CancelledEvents { get; set; } = Array.Empty<ContractCancelled>();
        public ContractFinished[] FinishedEvents { get; set; } = Array.Empty<ContractFinished>();
    }
}
