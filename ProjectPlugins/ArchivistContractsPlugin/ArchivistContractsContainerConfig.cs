using GethPlugin;

namespace ArchivistContractsPlugin
{
    public class ArchivistContractsContainerConfig
    {
        public ArchivistContractsContainerConfig(IGethNode gethNode)
        {
            GethNode = gethNode;
        }

        public IGethNode GethNode { get; }
    }
}
