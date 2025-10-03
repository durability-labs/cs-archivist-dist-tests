using Utils;

namespace TraceContract
{
    public class Config
    {
        public string DataDir { get; } = "tracecontract_datadir";
        public string ElasticSearchUsername { get; } = EnvVar.GetOrThrow("ES_USERNAME");
        public string ElasticSearchPassword { get; } = EnvVar.GetOrThrow("ES_PASSWORD");
        public string OuputFolder { get; } = EnvVar.GetOrThrow("OUTPUT_FOLDER");

        /// <summary>
        /// Naming things is hard.
        /// If the storage request is created at T=0, then fetching of the
        /// storage node logs will begin from T=0 minus 'LogStartBeforeStorageContractStarts'.
        /// </summary>
        public TimeSpan LogStartBeforeStorageContractStarts { get; } = TimeSpan.FromMinutes(1.0);
    }
}
