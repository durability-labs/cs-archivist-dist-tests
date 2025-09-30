using ArchivistContractsPlugin.Marketplace;
using Nethereum.Hex.HexConvertors.Extensions;
using Newtonsoft.Json;

namespace ArchivistContractsPlugin
{
    public interface IRequestsCache
    {
        void Add(byte[] requestId, Request request);
        Request? Get(byte[] requestId);
    }

    public class NullRequestsCache : IRequestsCache
    {
        public void Add(byte[] requestId, Request request)
        {
        }

        public Request? Get(byte[] requestId)
        {
            return null;
        }
    }

    public class DiskRequestsCache : IRequestsCache
    {
        private readonly string dataDir;

        public DiskRequestsCache(string dataDir)
        {
            this.dataDir = dataDir;
            Directory.CreateDirectory(dataDir);
        }

        public void Add(byte[] requestId, Request request)
        {
            var filename = FilePath(requestId);
            if (File.Exists(filename)) return;
            File.WriteAllText(filename, JsonConvert.SerializeObject(request));
        }

        public Request? Get(byte[] requestId)
        {
            var filename = FilePath(requestId);
            if (!File.Exists(filename)) return null;
            return JsonConvert.DeserializeObject<Request>(File.ReadAllText(filename));
        }

        private string FilePath(byte[] id)
        {
            return Path.Combine(dataDir, id.ToHex().ToLowerInvariant() + ".json");
        }
    }
}
