using ArchivistClient;
using ArchivistNetworkConfig;
using GatewayApi;
using Utils;

namespace GatewayClient
{
    public class GatewayClient
    {
        private readonly GatewayApiClient api;
        private readonly Mapper mapper = new Mapper();

        public GatewayClient(string baseUrl)
        {
            api = new GatewayApiClient(baseUrl, new HttpClient());
        }

        public GatewayClient(ArchivistNetwork network)
            : this(network.Team.Utils.Gateway)
        {
        }

        public LocalDataset GetManifest(ContentId cid)
        {
            return GetManifest(cid.Id);
        }

        public LocalDataset GetManifest(string cid)
        {
            var response = Time.Wait(api.ManifestAsync(cid));
            return mapper.Map(response);
        }
    }
}
