using ArchivistClient;
using ArchivistClient.Hooks;
using Core;

namespace ArchivistPlugin
{
    public static class CoreInterfaceExtensions
    {
        public static IArchivistInstance[] DeployArchivistNodes(this CoreInterface ci, int number, Action<IArchivistSetup> setup)
        {
            return Plugin(ci).DeployArchivistNodes(number, setup);
        }

        public static IArchivistNodeGroup WrapArchivistContainers(this CoreInterface ci, IArchivistInstance[] instances)
        {
            return Plugin(ci).WrapArchivistContainers(instances);
        }

        public static IArchivistNode StartArchivistNode(this CoreInterface ci)
        {
            return ci.StartArchivistNodes(1)[0];
        }

        public static IArchivistNode StartArchivistNode(this CoreInterface ci, Action<IArchivistSetup> setup)
        {
            return ci.StartArchivistNodes(1, setup)[0];
        }

        public static IArchivistNodeGroup StartArchivistNodes(this CoreInterface ci, int number, Action<IArchivistSetup> setup)
        {
            var rc = ci.DeployArchivistNodes(number, setup);
            var result = ci.WrapArchivistContainers(rc);
            Plugin(ci).WireUpMarketplace(result, setup);
            return result;
        }

        public static IArchivistNodeGroup StartArchivistNodes(this CoreInterface ci, int number)
        {
            return ci.StartArchivistNodes(number, s => { });
        }

        public static void AddArchivistHooksProvider(this CoreInterface ci, IArchivistHooksProvider hooksProvider)
        {
            Plugin(ci).AddArchivistHooksProvider(hooksProvider);
        }

        private static ArchivistPlugin Plugin(CoreInterface ci)
        {
            return ci.GetPlugin<ArchivistPlugin>();
        }
    }
}
