using NUnit.Framework;
using Logging;
using Utils;
using Core;
using ArchivistPlugin;
using ArchivistClient;

namespace ContinuousTests
{
    public class NodeRunner
    {
        private readonly EntryPointFactory entryPointFactory = new EntryPointFactory();
        private readonly IArchivistNode[] nodes;
        private readonly Configuration config;
        private readonly ILog log;
        private readonly string customNamespace;

        public NodeRunner(IArchivistNode[] nodes, Configuration config, ILog log, string customNamespace)
        {
            this.nodes = nodes;
            this.config = config;
            this.log = log;
            this.customNamespace = customNamespace;
        }

        public void RunNode(Action<IArchivistSetup> setup, Action<IArchivistNode> operation)
        {
            RunNode(nodes.ToList().PickOneRandom(), setup, operation);
        }

        public void RunNode(IArchivistNode bootstrapNode, Action<IArchivistSetup> setup, Action<IArchivistNode> operation)
        {
            var entryPoint = CreateEntryPoint();

            try
            {
                var debugInfo = bootstrapNode.GetDebugInfo();
                Assert.That(!string.IsNullOrEmpty(debugInfo.Spr));

                var node = entryPoint.CreateInterface().StartArchivistNode(s =>
                {
                    setup(s);
                    s.WithBootstrapNode(bootstrapNode);
                });

                try
                {
                    operation(node);
                }
                catch
                {
                    node.DownloadLog();
                    throw;
                }
            }
            finally
            {
                entryPoint.Tools.CreateWorkflow().DeleteNamespace(wait: false);
            }
        }

        private EntryPoint CreateEntryPoint()
        {
            return entryPointFactory.CreateEntryPoint(config.KubeConfigFile, config.DataPath, customNamespace, log);
        }
    }
}
