using ChainStateAPI.Controllers;
using ChainStateAPI.Database;
using Logging;
using Microsoft.EntityFrameworkCore;

namespace ChainStateAPI.Services
{
    public interface IContractStateService
    {
        ContractState? GetContractState(string contractId, DateTime utc);
    }

    public class ContractStateService : IContractStateService
    {
        private readonly ILog log;
        private readonly IDatabaseService databaseService;
        private readonly IMapperService mapper;

        public ContractStateService(ILog log, IDatabaseService databaseService, IMapperService mapper)
        {
            this.log = new LogPrefixer(log, "(ContractState) ");
            this.databaseService = databaseService;
            this.mapper = mapper;
        }

        public ContractState? GetContractState(string contractId, DateTime utc)
        {
            log.Log("Querying contract + events...");
            var oneOrZero = databaseService.Query<DbContractsContext, StorageContract[]>(context =>
            {
                return context.StorageContracts
                    .Where(c => c.RequestId == contractId)
                    .Where(c => c.CreationUtc < utc)
                    .Include(c => c.ContractStartedEvents)
                    .Include(c => c.ContractCancelledEvents)
                    .Include(c => c.ContractFinishedEvents)
                    .Include(c => c.ContractFailedEvents)
                    .Include(c => c.SlotFilledEvents)
                    .Include(c => c.SlotFreedEvents)
                    .ToArray();
            });

            if (oneOrZero.Length == 0) return null;
            var contract = oneOrZero[0];

            log.Log("Constructing state at given utc...");
            var result = new ContractState
            {
                ContractId = contractId,
                ContractChainValues = mapper.Map(contract),
                ContractDerivedValues = CreateDerivedValues(contract, utc)
            };

            log.Log("State constructed.");
            return result;
        }

        private ContractDerivedValues CreateDerivedValues(StorageContract contract, DateTime utc)
        {
            return new ContractDerivedValues
            {
                CreationUtc = contract.CreationUtc,
                ExpiryUtc   = contract.ExpiryUtc,
                FinishedUtc = contract.FinishedUtc, 
                ExtendsPreviousContracts = FindPreviousExtendedContracts(contract),
                Slots = CreateSlotsState(contract),
                State = DetermineState(contract, utc),
            };
        }

        private static RequestState DetermineState(StorageContract contract, DateTime utc)
        {
            // These are terminal events. If any of them appear, they define the state.
            if (AnyBefore(utc, contract.ContractFinishedEvents)) return RequestState.Finished;
            if (AnyBefore(utc, contract.ContractCancelledEvents)) return RequestState.Cancelled;
            if (AnyBefore(utc, contract.ContractFailedEvents)) return RequestState.Failed;

            // This one will become a terminal state at a later moment in time.
            if (AnyBefore(utc, contract.ContractStartedEvents)) return RequestState.Started;
            
            // The contract exists but has not moved from its initial creation state.
            return RequestState.New;
        }

        private static bool AnyBefore<T>(DateTime utc, ICollection<T> collection) where T : ContractEvent
        {
            return collection.Any(e => e.Utc < utc);
        }

        private ContractSlot[] CreateSlotsState(StorageContract contract)
        {
            var result = new List<ContractSlot>();
            for (ulong i = 0; i < contract.Ask.Slots; i++)
            {
                result.Add(CreateSlotState(contract, i));
            }
            return result.ToArray();
        }

        private ContractSlot CreateSlotState(StorageContract contract, ulong i)
        {
            return new ContractSlot
            {
                SlotIndex = i,
                HostOrEmpty = GetHostOrEmpty(contract, i)
            };
        }

        private static string GetHostOrEmpty(StorageContract contract, ulong i)
        {
            var fills = contract.SlotFilledEvents.Where(e => e.SlotIndex == i).ToArray();
            var frees = contract.SlotFreedEvents.Where(e => e.SlotIndex == i).ToArray();

            // If there are no fills, this is easy:
            if (fills.Length == 0) return string.Empty;

            // Now we need to look for the most recent fill or free.
            var latestFill = fills.Single(f => f.Utc == fills.Max(e => e.Utc));
            if (frees.Length == 0) return latestFill.HostAddress;

            var latestFree = frees.Single(f => f.Utc == frees.Max(e => e.Utc));

            // which was later, the fill or the free?
            if (latestFill.Utc > latestFree.Utc) return latestFill.HostAddress;
            return string.Empty;
        }

        private string[] FindPreviousExtendedContracts(StorageContract contract)
        {
            return databaseService.Query<DbContractsContext, string[]>(context =>
            {
                return context.StorageContracts
                    .Where(c => c.CidStr == contract.CidStr)
                    .Where(c => c.CreationUtc < contract.CreationUtc)
                    .Where(c => c.ContractStartedEvents.Any())
                    .Select(c => c.RequestId)
                    .ToArray();
            });
        }
    }
}
