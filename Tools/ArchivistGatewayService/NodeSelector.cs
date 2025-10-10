using Logging;

namespace ArchivistGatewayService
{
    public class NodeSelector
    {
        private readonly ILog log;
        private readonly Configuration config;
        private readonly List<string> nodeEndpoints = new();

        public NodeSelector(ILog log, Configuration config)
        {
            this.log = new LogPrefixer(log, "(NodeSelector)");
            this.config = config;
        }

        public async Task Initialize()
        {
            Log("Initializing...");

            nodeEndpoints.AddRange(config.ArchivistEndpoints.Split(";", StringSplitOptions.RemoveEmptyEntries));
            using var client = new HttpClient();
            foreach (var n in nodeEndpoints) await CheckEndpoint(n, client);

            Log("Ready");
        }

        public string GetNodeUrl(string cid)
        {
            Log("getting url for " + cid);
            return "http://192.168.178.26:8081/api/archivist/v1/";
        }

        private async Task CheckEndpoint(string endpoint, HttpClient client)
        {
            var infoUrl = $"{endpoint}/api/archivist/v1/debug/info";
            Log($"Checking node at '{infoUrl}'...");

            var response = await client.GetAsync(infoUrl);
            var str = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(str))
            {
                Log($"Invalid response received");
                throw new Exception($"Invalid response received from endpoint '{endpoint}'");
            }
        }

        private void Log(string msg)
        {
            log.Log(msg);
        }
    }
}
