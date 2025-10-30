using ArgsUniform;

namespace EphemeralClient
{
    public class Configuration
    {
        [Uniform("metrics-port", "mp", "METRICSPORT", false, "Local port for metrics endpoint. (default: '8008')")]
        public int MetricsPort { get; set; } = 8008;
    }
}
