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
            return WrapArchivistContractsDeployment(ci, gethNode, deployment, new NullRequestsCache());
        }

        public static IArchivistContracts WrapArchivistContractsDeployment(this CoreInterface ci, IGethNode gethNode, ArchivistContractsDeployment deployment, IRequestsCache requestsCache)
        {
            return Plugin(ci).WrapDeploy(gethNode, deployment, requestsCache);
        }

        public static IArchivistContracts StartArchivistContracts(this CoreInterface ci, IGethNode gethNode, DebugInfoVersion versionInfo)
        {
            var deployment = DeployArchivistContracts(ci, gethNode, versionInfo);
            return WrapArchivistContractsDeployment(ci, gethNode, deployment);
        }

        public static IArchivistContracts StartArchivistContracts(this CoreInterface ci, IGethNode gethNode, DebugInfoVersion versionInfo, IRequestsCache requestsCache)
        {
            var deployment = DeployArchivistContracts(ci, gethNode, versionInfo);
            return WrapArchivistContractsDeployment(ci, gethNode, deployment, requestsCache);
        }

        private static ArchivistContractsPlugin Plugin(CoreInterface ci)
        {
            return ci.GetPlugin<ArchivistContractsPlugin>();
        }
    }
}
