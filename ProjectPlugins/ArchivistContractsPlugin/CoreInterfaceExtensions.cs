using ArchivistClient;
using Core;
using GethPlugin;

namespace ArchivistContractsPlugin
{
    public static class CoreInterfaceExtensions
    {
        public static ArchivistContractsDeployment DeployArchivistContracts(this CoreInterface ci, IGethNode gethNode, DebugInfoVersion versionInfo)
        {
            return Plugin(ci).DeployContracts(ci, gethNode, versionInfo);
        }

        public static IArchivistContracts WrapArchivistContractsDeployment(this CoreInterface ci, IGethNode gethNode, ArchivistContractsDeployment deployment)
        {
            return Plugin(ci).WrapDeploy(gethNode, deployment);
        }

        public static IArchivistContracts StartArchivistContracts(this CoreInterface ci, IGethNode gethNode, DebugInfoVersion versionInfo)
        {
            var deployment = DeployArchivistContracts(ci, gethNode, versionInfo);
            return WrapArchivistContractsDeployment(ci, gethNode, deployment);
        }

        private static ArchivistContractsPlugin Plugin(CoreInterface ci)
        {
            return ci.GetPlugin<ArchivistContractsPlugin>();
        }
    }
}
