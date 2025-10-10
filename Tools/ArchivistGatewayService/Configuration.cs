using ArgsUniform;

namespace ArchivistGatewayService
{
    public class Configuration
    {
        [Uniform("archivist-endpoints", "ce", "ARCHIVISTENDPOINTS", false, "Archivist endpoints. Semi-colon separated. (default 'http://localhost:8080')")]
        public string ArchivistEndpoints { get; set; } =
            "http://localhost:8080" + ";" +
            "http://localhost:8081" + ";" +
            "http://localhost:8082" + ";" +
            "http://localhost:8083" + ";" +
            "http://localhost:8084" + ";" +
            "http://localhost:8085" + ";" +
            "http://localhost:8086" + ";" +
            "http://localhost:8087";

        [Uniform("datapath", "dp", "DATAPATH", false, "Root path where all data files will be saved.")]
        public string DataPath { get; set; } = "datapath";

        [Uniform("timeout", "t", "TIMEOUT", false, "Timeout (in minutes) used for outgoing HTTP requests.")]
        public int RequestTimeoutMinutes { get; set; } = 30;
    }
}
