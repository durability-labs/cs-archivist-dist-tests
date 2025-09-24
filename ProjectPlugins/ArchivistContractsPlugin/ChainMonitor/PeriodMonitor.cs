using ArchivistContractsPlugin.Marketplace;
using GethPlugin;
using Logging;
using Nethereum.Hex.HexConvertors.Extensions;
using Utils;

namespace ArchivistContractsPlugin.ChainMonitor
{
    public interface IPeriodMonitorEventHandler
    {
        void OnPeriodReport(PeriodReport report);
    }

    public class PeriodMonitor
    {
        private readonly ILog log;
        private readonly IArchivistContracts contracts;
        private readonly IGethNode geth;
        private readonly IPeriodMonitorEventHandler eventHandler;
        private readonly List<PeriodReport> reports = new List<PeriodReport>();
        private CurrentPeriod? currentPeriod = null;

        public PeriodMonitor(ILog log, IArchivistContracts contracts, IGethNode geth, IPeriodMonitorEventHandler eventHandler)
        {
            this.log = log;
            this.contracts = contracts;
            this.geth = geth;
            this.eventHandler = eventHandler;
        }

        public void Update(DateTime eventUtc, IChainStateRequest[] requests)
        {
            // It's possible that several periods have elapsed since our last update call.
            // If that's true, we'll take small time steps and roll towards 'eventUtc'.
            if (currentPeriod == null)
            {
                UpdateInternal(eventUtc, requests);
                return;
            }

            var utc = contracts.GetPeriodTimeRange(currentPeriod.PeriodNumber).From;
            while (utc < eventUtc)
            {
                // Repeat calls in the same period are ignored by updateInternal.
                UpdateInternal(utc, requests);
                utc += TimeSpan.FromSeconds(10);
            }
        }

        private void UpdateInternal(DateTime utc, IChainStateRequest[] requests)
        {
            var periodNumber = contracts.GetPeriodNumber(utc);
            if (currentPeriod == null)
            {
                currentPeriod = CreateCurrentPeriod(periodNumber, requests);
                return;
            }
            if (periodNumber == currentPeriod.PeriodNumber) return;

            CreateReportForPeriod(currentPeriod, requests);
            currentPeriod = CreateCurrentPeriod(periodNumber, requests);
        }

        public PeriodMonitorResult GetAndClearReports()
        {
            var result = reports.ToArray();
            reports.Clear();
            return new PeriodMonitorResult(result);
        }

        private CurrentPeriod CreateCurrentPeriod(ulong periodNumber, IChainStateRequest[] requests)
        {
            var cRequests = requests.Select(r => CurrentRequest.CreateCurrentRequest(contracts, r)).ToArray();
            return new CurrentPeriod(periodNumber, cRequests);
        }

        private void CreateReportForPeriod(CurrentPeriod currentPeriod, IChainStateRequest[] requests)
        {
            // Fetch function calls during period. Format report.
            var timeRange = contracts.GetPeriodTimeRange(currentPeriod.PeriodNumber);
            var blockRange = geth.ConvertTimeRangeToBlockRange(timeRange);

            var (callReports, markMissingCalls) = FetchCallReports(blockRange);

            var requestReports = new List<PeriodRequestReport>();
            var currentRemaining = currentPeriod.CurrentRequests.ToList();
            foreach (var r in requests)
            {
                var current = TakeMatchingCurrent(currentRemaining, r);
                if (current == null)
                {
                    // Request is new during this period.
                    requestReports.Add(PeriodRequestReport.CreatePeriodRequestReport(contracts, r));
                }
                else
                {
                    // Request existed at start of period.
                    var hasEnded = r.State == RequestState.Cancelled || r.State == RequestState.Finished || r.State == RequestState.Failed;
                    requestReports.Add(PeriodRequestReport.CreatePeriodRequestReport(
                        contracts,
                        currentPeriod.PeriodNumber,
                        current,
                        hasEnded,
                        markMissingCalls));
                }
            }
            // If there remain currentRequests, those seem to have vanished from the chainstate
            // (That's odd. This shouldn't happen. We should log this as a warning.)
            // We'll consider them ended during this period.
            foreach (var remaining in currentRemaining)
            {
                log.Log($"Warning: A request disappeared from the ChainState. This shouldn't happen. requestId:{remaining.Request.RequestId.ToHex()}");
                requestReports.Add(PeriodRequestReport.CreatePeriodRequestReport(
                        contracts,
                        currentPeriod.PeriodNumber,
                        remaining,
                        hasEnded: true,
                        markMissingCalls));
            }

            var report = new PeriodReport(
                new ProofPeriod(currentPeriod.PeriodNumber, timeRange, blockRange),
                requestReports.ToArray(),
                callReports.ToArray());

            report.Log(log);
            reports.Add(report);

            eventHandler.OnPeriodReport(report);
        }

        private CurrentRequest? TakeMatchingCurrent(List<CurrentRequest> currentRemaining, IChainStateRequest r)
        {
            var current = currentRemaining.ToArray();
            foreach (var c in current)
            {
                if (c.Request.RequestId.ToHex() == r.RequestId.ToHex())
                {
                    currentRemaining.Remove(c);
                    return c;
                }
            }
            return null;
        }

        private (FunctionCallReport[], MarkProofAsMissingFunction[]) FetchCallReports(BlockInterval blockRange)
        {
            var callReports = new List<FunctionCallReport>();
            var missedCalls = new List<MarkProofAsMissingFunction>();
            geth.IterateTransactions(blockRange, (t, blkI, blkUtc) =>
            {
                var reporter = new CallReporter(callReports, t, blkUtc, blkI);
                reporter.Run(missedCalls.Add);

            });
            return (callReports.ToArray(), missedCalls.ToArray());
        }
    }

    public class DoNothingPeriodMonitorEventHandler : IPeriodMonitorEventHandler
    {
        public void OnPeriodReport(PeriodReport report)
        {
        }
    }
}
