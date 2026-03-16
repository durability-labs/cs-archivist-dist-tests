using ArgsUniform;

namespace ContractDataChecker
{
    public class Configuration
    {
        [Uniform("datapath", "dp", "DATAPATH", true, "Root path where all data files will be saved.")]
        public string DataPath { get; set; } = "datapath";
        
        [Uniform("archivist-endpoint", "ce", "ARCHIVISTENDPOINT", false, "Archivist endpoint. (default 'http://localhost:8080')")]
        public string ArchivistEndpoint { get; set; } = "http://localhost:8080";

        public string LogPath
        {
            get
            {
                return Path.Combine(DataPath, "logs");
            }
        }
    }
}
