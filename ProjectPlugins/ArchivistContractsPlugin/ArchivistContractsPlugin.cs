using Core;
using GethPlugin;

namespace ArchivistContractsPlugin
{
    public class ArchivistContractsPlugin : IProjectPlugin, IHasLogPrefix, IHasMetadata
    {
        private readonly IPluginTools tools;
        private readonly ArchivistContractsStarter starter;

        public ArchivistContractsPlugin(IPluginTools tools)
        {
            this.tools = tools;
            starter = new ArchivistContractsStarter(tools);
        }

        public string LogPrefix => "(ArchivistContracts) ";

        public void Awake(IPluginAccess access)
        {
        }

        public void Announce()
        {
            tools.GetLog().Log($"Loaded Archivist-Marketplace SmartContracts");
        }

        public void AddMetadata(IAddMetadata metadata)
        {
            metadata.Add("archivistcontractsid", "dynamic");
        }

        public void Decommission()
        {
        }

        public ArchivistContractsDeployment DeployContracts(CoreInterface ci, IGethNode gethNode, ArchivistClient.DebugInfoVersion versionInfo)
        {
            return starter.Deploy(ci, gethNode, versionInfo);
        }

        public IArchivistContracts WrapDeploy(IGethNode gethNode, ArchivistContractsDeployment deployment)
        {
            deployment = SerializeGate.Gate(deployment);
            return starter.Wrap(gethNode, deployment);
        }
    }
}
