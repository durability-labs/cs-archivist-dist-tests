using ArchivistContractsPlugin;
using ArchivistContractsPlugin.ChainMonitor;
using GethPlugin;
using Logging;
using Utils;

namespace ArchivistReleaseTests.Utils
{
    public class ChainMonitor
    {
        private readonly ILog log;
        private readonly IGethNode gethNode;
        private readonly IArchivistContracts contracts;
        private readonly IPeriodMonitorEventHandler periodMonitorEventHandler;
        private readonly DateTime startUtc;
        private readonly TimeSpan updateInterval;
        private CancellationTokenSource cts = new CancellationTokenSource();
        private Task worker = Task.CompletedTask;
        private bool monitorProofPeriods;

        public ChainMonitor(ILog log, IGethNode gethNode, IArchivistContracts contracts, IPeriodMonitorEventHandler periodMonitorEventHandler, DateTime startUtc, TimeSpan updateInterval, bool monitorProofPeriods)
        {
            this.log = log;
            this.gethNode = gethNode;
            this.contracts = contracts;
            this.periodMonitorEventHandler = periodMonitorEventHandler;
            this.startUtc = startUtc;
            this.updateInterval = updateInterval;
            this.monitorProofPeriods = monitorProofPeriods;
        }

        public void Start(Action onFailure)
        {
            cts = new CancellationTokenSource();
            worker = Task.Run(() => Worker(onFailure));
        }

        public void Stop()
        {
            cts.Cancel();
            worker.Wait();
            if (worker.Exception != null) throw worker.Exception;
        }

        public IChainStateRequest[] Requests { get; private set; } = Array.Empty<IChainStateRequest>();

        private void Worker(Action onFailure)
        {
            try
            {
                var state = new ChainState(log, gethNode, contracts, new DoNothingThrowingChainEventHandler(), startUtc, monitorProofPeriods, periodMonitorEventHandler);
                Thread.Sleep(updateInterval);

                log.Log($"Chain monitoring started. Update interval: {Time.FormatDuration(updateInterval)}");
                while (!cts.IsCancellationRequested)
                {
                    state.Update();
                    Requests = state.Requests.ToArray();
                    cts.Token.WaitHandle.WaitOne(updateInterval);
                }
            }
            catch (Exception ex)
            {
                log.Error("Exception in chain monitor: " + ex);
                onFailure();
                throw;
            }
        }
    }
}
