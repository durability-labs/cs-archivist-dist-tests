using ArchivistClient;
using ArchivistClient.Hooks;
using Core;

namespace ArchivistPlugin
{
    public class ArchivistPlugin : IProjectPlugin, IHasLogPrefix, IHasMetadata
    {
        private const bool UseContainers = true;

        private readonly IArchivistStarter archivistStarter;
        private readonly IPluginTools tools;
        private readonly ArchivistLogLevel defaultLogLevel = ArchivistLogLevel.Trace;
        private readonly ArchivistHooksFactory hooksFactory = new ArchivistHooksFactory();
        private readonly ProcessControlMap processControlMap = new ProcessControlMap();
        private readonly ArchivistDockerImage archivistDockerImage = new ArchivistDockerImage();
        private readonly ArchivistContainerRecipe recipe;
        private readonly ArchivistWrapper archivistWrapper;

        public ArchivistPlugin(IPluginTools tools)
        {
            this.tools = tools;

            recipe = new ArchivistContainerRecipe(archivistDockerImage);
            archivistStarter = CreateArchivistStarter();
            archivistWrapper = new ArchivistWrapper(tools, processControlMap, hooksFactory);
        }

        private IArchivistStarter CreateArchivistStarter()
        {
            if (UseContainers)
            {
                Log("Using Containerized Archivist instances");
                return new ContainerArchivistStarter(tools, recipe, processControlMap);
            }

            Log("Using Binary Archivist instances");
            return new BinaryArchivistStarter(tools, processControlMap);
        }

        public string LogPrefix => "(Archivist) ";

        public void Awake(IPluginAccess access)
        {
        }

        public void Announce()
        {
            // give archivist docker image to contracts plugin.

            Log($"Loaded with Archivist ID: '{archivistWrapper.GetArchivistId()}' - Revision: {archivistWrapper.GetArchivistRevision()}");
        }

        public void AddMetadata(IAddMetadata metadata)
        {
            metadata.Add("archivistid", archivistWrapper.GetArchivistId());
            metadata.Add("archivistrevision", archivistWrapper.GetArchivistRevision());
        }

        public void Decommission()
        {
            archivistStarter.Decommission();
        }

        public IArchivistInstance[] DeployArchivistNodes(int numberOfNodes, Action<IArchivistSetup> setup)
        {
            var archivistSetup = GetSetup(numberOfNodes, setup);
            return archivistStarter.BringOnline(archivistSetup);
        }

        public IArchivistNodeGroup WrapArchivistContainers(IArchivistInstance[] instances)
        {
            instances = instances.Select(c => SerializeGate.Gate(c as ArchivistInstance)).ToArray();
            return archivistWrapper.WrapArchivistInstances(instances);
        }

        public void WireUpMarketplace(IArchivistNodeGroup result, Action<IArchivistSetup> setup)
        {
            var archivistSetup = GetSetup(1, setup);
            if (archivistSetup.MarketplaceConfig == null) return;
            
            var mconfig = archivistSetup.MarketplaceConfig;
            foreach (var node in result)
            {
                mconfig.GethNode.SendEth(node, mconfig.MarketplaceSetup.InitialEth);
                mconfig.ArchivistContracts.MintTestTokens(node, mconfig.MarketplaceSetup.InitialTestTokens);

                Log($"Send {mconfig.MarketplaceSetup.InitialEth} and " +
                    $"minted {mconfig.MarketplaceSetup.InitialTestTokens} for " +
                    $"{node.GetName()} (address: {node.EthAddress})");
            }
        }

        public void AddArchivistHooksProvider(IArchivistHooksProvider hooksProvider)
        {
            if (hooksFactory.Providers.Contains(hooksProvider)) return;
            hooksFactory.Providers.Add(hooksProvider);
        }

        private ArchivistSetup GetSetup(int numberOfNodes, Action<IArchivistSetup> setup)
        {
            var archivistSetup = new ArchivistSetup(numberOfNodes);
            archivistSetup.LogLevel = defaultLogLevel;
            setup(archivistSetup);
            return archivistSetup;
        }

        private void Log(string msg)
        {
            tools.GetLog().Log(msg);
        }
    }
}
