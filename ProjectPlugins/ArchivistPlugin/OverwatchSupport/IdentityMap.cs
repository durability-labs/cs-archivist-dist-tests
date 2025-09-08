using ArchivistClient;

namespace ArchivistPlugin.OverwatchSupport
{
    public class IdentityMap
    {
        private readonly List<ArchivistNodeIdentity> nodes = new List<ArchivistNodeIdentity>();
        private readonly Dictionary<string, int> nameIndexMap = new Dictionary<string, int>();
        private readonly Dictionary<string, string> shortToLong = new Dictionary<string, string>();

        public void Add(string name, string peerId, string nodeId)
        {
            Add(new ArchivistNodeIdentity
            {
                Name = name,
                PeerId = peerId,
                NodeId = nodeId
            });

            nameIndexMap.Add(name, nameIndexMap.Count);
        }

        public void Add(ArchivistNodeIdentity identity)
        {
            if (string.IsNullOrWhiteSpace(identity.Name)) throw new Exception("Name required");
            if (string.IsNullOrWhiteSpace(identity.PeerId) || identity.PeerId.Length < 11) throw new Exception("PeerId invalid");
            if (string.IsNullOrWhiteSpace(identity.NodeId) || identity.NodeId.Length < 11) throw new Exception("NodeId invalid");

            nodes.Add(identity);

            shortToLong.Add(ArchivistUtils.ToShortId(identity.PeerId), identity.PeerId);
            shortToLong.Add(ArchivistUtils.ToNodeIdShortId(identity.NodeId), identity.NodeId);
        }

        public ArchivistNodeIdentity[] Get()
        {
            return nodes.ToArray();
        }

        public int GetIndex(string name)
        {
            return nameIndexMap[name];
        }

        public ArchivistNodeIdentity GetId(string name)
        {
            return nodes.Single(n => n.Name == name);
        }

        public string ReplaceShortIds(string value)
        {
            var result = value;
            foreach (var pair in shortToLong)
            {
                result = result.Replace(pair.Key, pair.Value);
            }
            return result;
        }

        public int Size
        {
            get { return nodes.Count; }
        }
    }
}
