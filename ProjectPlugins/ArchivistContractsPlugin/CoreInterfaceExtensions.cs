using Core;

namespace ArchivistContractsPlugin
{
    public static class CoreInterfaceExtensions
    {
        public static ArchivistContractsDeployment DeployArchivistContracts(this CoreInterface ci, Action<IArchivistContractsSetup> setup)
        {
            return Plugin(ci).DeployContracts(ci, setup);
        }

        public static IArchivistContracts WrapArchivistContractsDeployment(this CoreInterface ci, ArchivistContractsDeployment deployment, Action<IArchivistContractsSetup> setup)
        {
            return Plugin(ci).WrapDeploy(deployment, setup);
        }

        public static IArchivistContracts StartArchivistContracts(this CoreInterface ci, Action<IArchivistContractsSetup> setup)
        {
            var deployment = DeployArchivistContracts(ci, setup);
            return WrapArchivistContractsDeployment(ci, deployment, setup);
        }

        private static ArchivistContractsPlugin Plugin(CoreInterface ci)
        {
            return ci.GetPlugin<ArchivistContractsPlugin>();
        }
    }
}
