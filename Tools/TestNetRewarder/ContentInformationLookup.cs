using ArchivistNetworkConfig;

namespace TestNetRewarder
{
    public class ContentInformationLookup
    {
        private readonly GatewayClient.GatewayClient client;

        public ContentInformationLookup(ArchivistNetwork network)
        {
            client = new GatewayClient.GatewayClient(network);
        }

        public string[] LookUp(string cid)
        {
            if (string.IsNullOrEmpty(cid)) return Array.Empty<string>();

            try
            {
                return LookupContent(cid);
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        private string[] LookupContent(string cid)
        {
            var manifest = client.GetManifest(cid).Manifest;
            return
            [
                $"   Filename: '{manifest.Filename}'",
                $"   ContentType: {manifest.Mimetype}",
                $"   DatasetSize: {manifest.DatasetSize}"
            ];
        }
    }
}
