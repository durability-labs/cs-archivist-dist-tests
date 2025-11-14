using ArchivistContractsPlugin;
using ArchivistContractsPlugin.ChainMonitor;
using DiscordRewards;
using GethPlugin;
using Logging;
using Utils;

namespace TestNetRewarder
{
    public class Processor : ITimeSegmentHandler, IPeriodMonitorEventHandler
    {
        private readonly RequestBuilder builder;
        private readonly EventsFormatter eventsFormatter;
        private readonly ChainState chainState;
        private readonly Configuration config;
        private readonly BotClient client;
        private readonly ILog log;
        private DateTime lastPeriodUpdateUtc;

        public Processor(Configuration config, BotClient client, IGethNode geth, IArchivistContracts contracts, ILog log)
        {
            this.config = config;
            this.client = client;
            this.log = log;
            lastPeriodUpdateUtc = DateTime.UtcNow;

            builder = new RequestBuilder();
            eventsFormatter = new EventsFormatter(config, contracts.Deployment.Config);

            chainState = new ChainState(log, geth, contracts, eventsFormatter, config.HistoryStartUtc,
                doProofPeriodMonitoring: config.ShowProofsMissed > 0, this);
        }

        public async Task Initialize()
        {
            var events = eventsFormatter.GetInitializationEvents(config);
            var request = builder.Build(chainState, events, Array.Empty<string>());
            if (request.HasAny())
            {
                await client.SendRewards(request);
            }
        }

        public async Task<TimeSegmentResponse> OnNewSegment(TimeRange timeRange)
        {
            try
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var numberOfChainEvents = await ProcessEvents(timeRange);
                var duration = sw.Elapsed;

                log.Log($"{nameof(ProcessEvents)} = {Time.FormatDuration(duration)}");
                if (duration < TimeSpan.FromSeconds(1)) return TimeSegmentResponse.Underload;
                if (duration > TimeSpan.FromSeconds(3)) return TimeSegmentResponse.Overload;
                return TimeSegmentResponse.OK;
            }
            catch (Exception ex)
            {
                var msg = "Exception processing time segment: " + ex;
                log.Error(msg); 
                eventsFormatter.OnError(msg);
                throw;
            }
        }

        public bool GetLogPeriodReports()
        {
            return false;
        }

        public void OnPeriodReport(PeriodReport report)
        {
            var missedSlots = new List<MissedSlot>();

            foreach (var r in report.Requests)
            {
                foreach (var s in r.Slots)
                {
                    if (s.GetIsProofMissed())
                    {
                        missedSlots.Add(new MissedSlot(r.Request, s));
                    }
                }
            }

            eventsFormatter.OnPeriodReport(new PeriodReportWithMisses(report, missedSlots.ToArray()));
        }

        private async Task<int> ProcessEvents(TimeRange timeRange)
        {
            log.Log($"Processing time range: {timeRange}");
            try
            {
                var numberOfChainEvents = chainState.Update(timeRange.To);
                var events = eventsFormatter.GetEvents();
                var errors = eventsFormatter.GetErrors();

                var request = builder.Build(chainState, events, errors);
                if (request.HasAny())
                {
                    await client.SendRewards(request);
                }
                return numberOfChainEvents;
            }
            catch (Exception ex)
            {
                log.Error($"Failed to update chainState for time range {timeRange}: {ex}");
                return 0;
            }
        }
    }

    public class PeriodReportWithMisses
    {
        public PeriodReportWithMisses(PeriodReport periodReport, MissedSlot[] missedSlots)
        {
            PeriodReport = periodReport;
            MissedSlots = missedSlots;
        }

        public PeriodReport PeriodReport { get; }
        public MissedSlot[] MissedSlots { get; }
    }

    public class MissedSlot
    {
        public MissedSlot(IChainStateRequest request, PeriodRequestSlotReport slotReport)
        {
            Request = request;
            SlotReport = slotReport;
        }

        public IChainStateRequest Request { get; }
        public PeriodRequestSlotReport SlotReport { get; }
    }
}
