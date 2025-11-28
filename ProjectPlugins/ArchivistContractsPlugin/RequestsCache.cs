using ArchivistContractsPlugin.Marketplace;
using Nethereum.Hex.HexConvertors.Extensions;
using Newtonsoft.Json;

namespace ArchivistContractsPlugin
{
    public interface IRequestsCache
    {
        void Add(byte[] requestId, Request request);
        Request? Get(byte[] requestId);
        void Delete(byte[] requestId);
        void IterateAll(Action<byte[]> onRequestId);
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

        public void Delete(byte[] requestId)
        {
        }

        public void IterateAll(Action<byte[]> onRequestId)
        {
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

            try
            {
                var text = File.ReadAllText(filename);
                return JsonConvert.DeserializeObject<Request>(text);
            }
            catch
            {
                File.Delete(filename);
                return null;
            }
        }

        public void Delete(byte[] requestId)
        {
            var filename = FilePath(requestId);
            if (File.Exists(filename)) File.Delete(filename);
        }

        public void IterateAll(Action<byte[]> onRequestId)
        {
            var files = Directory.GetFiles(dataDir);
            foreach (var f in files)
            {
                var filename = Path.GetFileName(f);
                if (filename.EndsWith(".json"))
                {
                    try
                    {
                        var id = filename.Substring(0, filename.Length - 5).HexToByteArray();
                        onRequestId(id);
                    }
                    catch
                    {
                    }
                }
            }
        }

        private string FilePath(byte[] id)
        {
            return Path.Combine(dataDir, id.ToHex().ToLowerInvariant() + ".json");
        }
    }
}
