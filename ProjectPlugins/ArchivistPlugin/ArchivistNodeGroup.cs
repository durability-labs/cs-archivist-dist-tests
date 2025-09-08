using ArchivistClient;
using Core;
using System.Collections;
using Utils;

namespace ArchivistPlugin
{
    public interface IArchivistNodeGroup : IEnumerable<IArchivistNode>, IHasManyMetricScrapeTargets
    {
        void Stop(bool waitTillStopped);
        IArchivistNode this[int index] { get; }
    }

    public class ArchivistNodeGroup : IArchivistNodeGroup
    {
        private readonly IArchivistNode[] nodes;

        public ArchivistNodeGroup(IPluginTools tools, IArchivistNode[] nodes)
        {
            this.nodes = nodes;
            Version = new DebugInfoVersion();
        }

        public IArchivistNode this[int index]
        {
            get
            {
                return Nodes[index];
            }
        }

        public void Stop(bool waitTillStopped)
        {
            foreach (var node in Nodes) node.Stop(waitTillStopped);
        }

        public void Stop(ArchivistNode node, bool waitTillStopped)
        {
            node.Stop(waitTillStopped);
        }

        public IArchivistNode[] Nodes => nodes;
        public DebugInfoVersion Version { get; private set; }

        public Address[] GetMetricsScrapeTargets()
        {
            return Nodes.Select(n => n.GetMetricsScrapeTarget()).ToArray();
        }

        public IEnumerator<IArchivistNode> GetEnumerator()
        {
            return Nodes.Cast<IArchivistNode>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Nodes.GetEnumerator();
        }

        public string Names()
        {
            return $"[{string.Join(",", Nodes.Select(n => n.GetName()))}]";
        }

        public override string ToString()
        {
            return Names();
        }

        public void EnsureOnline()
        {
            var versionResponses = Nodes.Select(n => n.Version);

            var first = versionResponses.First();
            if (!versionResponses.All(v => v.Version == first.Version && v.Revision == first.Revision))
            {
                throw new Exception("Inconsistent version information received from one or more Archivist nodes: " +
                    string.Join(",", versionResponses.Select(v => v.ToString())));
            }

            Version = first;
        }
    }

    public static class ArchivistNodeGroupExtensions
    {
        public static string Names(this IArchivistNode[] nodes)
        {
            return $"[{string.Join(",", nodes.Select(n => n.GetName()))}]";
        }

        public static string Names(this List<IArchivistNode> nodes)
        {
            return $"[{string.Join(",", nodes.Select(n => n.GetName()))}]";
        }
    }
}
