using ArgsUniform;

namespace EphemeralClient
{
    public class Configuration
    {
        [Uniform("metrics-port", "mp", "METRICSPORT", false, "Local port for metrics endpoint. (default: '8008')")]
        public int MetricsPort { get; set; } = 8008;

        [Uniform("filesize-mb", "fmb", "FILESIZEMB", false, "Size in megabytes of file to use for gateway testing. (default: '10')")]
        public int FilesizeMb { get; set; } = 10;
    }
}
