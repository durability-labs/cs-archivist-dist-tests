using Logging;
using Prometheus;

namespace ArchivistGatewayService
{
    public class AppMetrics
    {
        private readonly ILog log;
        private readonly MetricServer server;

        public AppMetrics(ILog log)
        {
            this.log = log;
            server = new MetricServer(1234);
        }
        
        public void Initialize()
        {
            server.Start();

            var g = Metrics.CreateGauge("name", "help");

            g.Set(12.34);
        }
    }
}
