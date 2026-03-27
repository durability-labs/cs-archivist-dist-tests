using ChainStateAPI.Database;
using Logging;
using Utils;

namespace ChainStateAPI.Services
{
    public interface IActiveContractsService
    {
        string[] GetActiveContractIds(DateTime utc);
    }

    public class ActiveContractsService : IActiveContractsService
    {
        private readonly ILog log;
        private readonly IDatabaseService databaseService;

        public ActiveContractsService(ILog log, IDatabaseService databaseService)
        {
            this.log = new LogPrefixer(log, "(ActiveContracts) ");
            this.databaseService = databaseService;
        }

        public string[] GetActiveContractIds(DateTime utc)
        {
            log.Log($"Getting active contracts at {Time.FormatTimestamp(utc)}...");
            var result = databaseService.Query<DbContractsContext, string[]>(context =>
            {
                // These contracts have a creation before the UTC and a finish after.
                var open = context.StorageContracts.Where(c => 
                    c.CreationUtc < utc &&
                    c.FinishedUtc > utc
                ).ToArray();

                var active = open.Where(c =>
                {
                    // If they were started before the UTC and not failed, they are active!
                    return
                        c.ContractStartedEvents.Any(e => e.Utc < utc) &&
                        !c.ContractFailedEvents.Any(e => e.Utc < utc);
                });

                return active
                    .Select(a => a.RequestId)
                    .ToArray();
            });
            log.Log($"Found {result.Length} active contracts at {Time.FormatTimestamp(utc)}.");
            return result;
        }
    }
}
