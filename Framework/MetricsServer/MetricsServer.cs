using Logging;
using Prometheus;
using Utils;

namespace MetricsServer
{
    public class MetricsServer
    {
        private readonly ILog log;
        private readonly int serverPort;
        private readonly string metricsNamePrefix;
        private readonly TimeSpan eventDuration;
        private MetricServer server = null!;

        public MetricsServer(ILog log, int serverPort, string metricsNamePrefix)
            : this(log, serverPort, metricsNamePrefix, TimeSpan.FromMinutes(30))
        {
        }

        public MetricsServer(ILog log, int serverPort, string metricsNamePrefix, TimeSpan eventDuration)
        {
            this.log = new LogPrefixer(log, "(MetricsServer)");
            this.serverPort = serverPort;
            this.metricsNamePrefix = metricsNamePrefix;
            this.eventDuration = eventDuration;
        }

        public void Start()
        {
            log.Log($"Creating metrics server at port {serverPort}...");
            log.Log($"Using prefix '{metricsNamePrefix}' and event duration: {Time.FormatDuration(eventDuration)}");
            server = new MetricServer(serverPort);
            server.Start();
        }

        public MetricsGauge CreateGauge(string gaugeName, string description)
        {
            log.Log($"Created new gauge: {gaugeName}");
            return new MetricsGauge(
                Metrics.CreateGauge(
                    $"{metricsNamePrefix}_{gaugeName}",
                    description)
            );
        }

        public MetricsEvent CreateEvent(string eventName, string description)
        {
            log.Log($"Created new event: {eventName}");
            return new MetricsEvent(
                log,
                Metrics.CreateGauge(
                    $"{metricsNamePrefix}_{eventName}",
                    $"{description} (last {Time.FormatDuration(eventDuration)})"),
                eventDuration
            );
        }

        public void Stop()
        {
            server.Stop();
        }
    }
}
