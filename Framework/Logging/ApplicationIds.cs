namespace Logging
{
    public class ApplicationIds
    {
        public ApplicationIds(string archivistId, string gethId, string prometheusId, string archivistContractsId, string grafanaId)
        {
            ArchivistId = archivistId;
            GethId = gethId;
            PrometheusId = prometheusId;
            ArchivistContractsId = archivistContractsId;
            GrafanaId = grafanaId;
        }

        public string ArchivistId { get; }
        public string GethId { get; }
        public string PrometheusId { get; }
        public string ArchivistContractsId { get; }
        public string GrafanaId { get; }
    }
}
