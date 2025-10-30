using Prometheus;

namespace MetricsServer
{
    public class MetricsGauge
    {
        private readonly Gauge gauge;

        internal MetricsGauge(Gauge gauge)
        {
            this.gauge = gauge;
        }

        public void Set(int value)
        {
            gauge.Set(value);
        }
    }
}
