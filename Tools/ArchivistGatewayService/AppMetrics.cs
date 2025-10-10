using Logging;
using Prometheus;
using Utils;

namespace ArchivistGatewayService
{
    public class AppMetrics
    {
        private const string MetricPrefix = "archivist_gateway_";
        private readonly TimeSpan SampleDuration = TimeSpan.FromHours(6.0);
        private readonly TimeSpan LoopDelay = TimeSpan.FromHours(0.5);
        private readonly ILog log;
        private readonly NodeSelector nodes;
        private readonly MetricServer server;
        private readonly object _lock = new object();
        private readonly List<DateTime> manifestRequestUtcs = new List<DateTime>();
        private readonly List<DateTime> dataRequestUtcs = new List<DateTime>();
        private readonly List<DateTime> checksRequestUtcs = new List<DateTime>();
        private Gauge manifestGauge = null!;
        private Gauge dataGauge = null!;
        private Gauge checkGauge = null!;

        public AppMetrics(ILog log, Configuration config, NodeSelector nodes)
        {
            this.log = log;
            this.nodes = nodes;
            log.Log($"Creating metrics server at port {config.MetricsPort}...");
            server = new MetricServer(config.MetricsPort);
        }
        
        public void Initialize()
        {
            server.Start();

            manifestGauge = Metrics.CreateGauge($"{MetricPrefix}manifests", $"Number of requests to manifest endpoint in last {Time.FormatDuration(SampleDuration)}");
            dataGauge = Metrics.CreateGauge($"{MetricPrefix}data", $"Number of requests to data endpoint in last {Time.FormatDuration(SampleDuration)}");
            checkGauge = Metrics.CreateGauge($"{MetricPrefix}checks", $"Number of connection checks in last {Time.FormatDuration(SampleDuration)}");

            Task.Run(Worker);
        }

        public void OnManifestRequest()
        {
            lock (_lock)
            {
                manifestRequestUtcs.Add(DateTime.UtcNow);
                manifestGauge.Set(manifestRequestUtcs.Count);
            }
        }

        public void OnDataRequest()
        {
            lock (_lock)
            {
                dataRequestUtcs.Add(DateTime.UtcNow);
                dataGauge.Set(dataRequestUtcs.Count);
            }
        }

        private void Worker()
        {
            while (true)
            {
                try
                {
                    WorkerStep();
                }
                catch (Exception ex)
                {
                    log.Error($"Error in worker thread: {ex}");
                }
                Thread.Sleep(LoopDelay);
            }
        }

        private void WorkerStep()
        {
            lock (_lock)
            {
                TimeoutEntries(manifestRequestUtcs);
                TimeoutEntries(dataRequestUtcs);
                TimeoutEntries(checksRequestUtcs);

                manifestGauge.Set(manifestRequestUtcs.Count);
                dataGauge.Set(dataRequestUtcs.Count);
                checkGauge.Set(checksRequestUtcs.Count);
            }

            try
            {
                nodes.CheckOneNode().Wait();
                OnCheck();
            }
            catch
            {
                // Quietly ignore. We'll see it in the metrics.
            }
        }

        private void TimeoutEntries(List<DateTime> utcs)
        {
            utcs.RemoveAll(utc => utc > (DateTime.UtcNow - SampleDuration));
        }

        private void OnCheck()
        {
            lock (_lock)
            {
                checksRequestUtcs.Add(DateTime.UtcNow);
                checkGauge.Set(checksRequestUtcs.Count);
            }
        }
    }
}
