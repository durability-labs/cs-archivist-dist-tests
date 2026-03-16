using ArgsUniform;

namespace ContractDataChecker
{
    public class Configuration
    {
        private readonly DateTime AppStartUct = DateTime.UtcNow;

        [Uniform("datapath", "dp", "DATAPATH", true, "Root path where all data files will be saved.")]
        public string DataPath { get; set; } = "datapath";
        
        [Uniform("archivist-endpoint", "ce", "ARCHIVISTENDPOINT", false, "Archivist endpoint. (default 'http://localhost:8080')")]
        public string ArchivistEndpoint { get; set; } = "http://localhost:8080";

        [Uniform("interval-minutes", "im", "INTERVALMINUTES", true, "time in minutes between reward updates.")]
        public int IntervalMinutes { get; set; } = 2;

        [Uniform("relative-history", "rh", "RELATIVEHISTORY", false, "Number of seconds into the past (from app start) that checking of chain history will start. Default: 3 hours ago.")]
        public int RelativeHistorySeconds { get; set; } = 3600 * 3;

        public string LogPath
        {
            get
            {
                return Path.Combine(DataPath, "logs");
            }
        }

        public TimeSpan Interval
        {
            get
            {
                return TimeSpan.FromMinutes(Math.Max(IntervalMinutes, 15));
            }
        }

        public DateTime HistoryStartUtc
        {
            get
            {
                return AppStartUct - TimeSpan.FromSeconds(RelativeHistorySeconds);
            }
        }
    }
}
