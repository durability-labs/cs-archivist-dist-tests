using ArchivistClient;
using ArchivistContractsPlugin;
using GethPlugin;
using KubernetesWorkflow.Types;

namespace ArchivistPlugin
{
    public class ArchivistDeployment
    {
        public ArchivistDeployment(ArchivistInstance[] archivistInstances, GethDeployment gethDeployment,
            ArchivistContractsDeployment archivistContractsDeployment, RunningPod? prometheusContainer,
            RunningPod? discordBotContainer, DeploymentMetadata metadata,
            string id)
        {
            Id = id;
            ArchivistInstances = archivistInstances;
            GethDeployment = gethDeployment;
            ArchivistContractsDeployment = archivistContractsDeployment;
            PrometheusContainer = prometheusContainer;
            DiscordBotContainer = discordBotContainer;
            Metadata = metadata;
        }

        public string Id { get; }
        public ArchivistInstance[] ArchivistInstances { get; }
        public GethDeployment GethDeployment { get; }
        public ArchivistContractsDeployment ArchivistContractsDeployment { get; }
        public RunningPod? PrometheusContainer { get; }
        public RunningPod? DiscordBotContainer { get; }
        public DeploymentMetadata Metadata { get; }
    }

    public class DeploymentMetadata
    {
        public DeploymentMetadata(string name, DateTime startUtc, DateTime finishedUtc, string kubeNamespace,
            int numberOfArchivistNodes, int numberOfValidators, int storageQuotaMB, ArchivistLogLevel archivistLogLevel,
            int initialTestTokens, int minPrice, int maxCollateral, int maxDuration, int blockTTL, int blockMI,
            int blockMN)
        {
            Name = name;
            StartUtc = startUtc;
            FinishedUtc = finishedUtc;
            KubeNamespace = kubeNamespace;
            NumberOfArchivistNodes = numberOfArchivistNodes;
            NumberOfValidators = numberOfValidators;
            StorageQuotaMB = storageQuotaMB;
            ArchivistLogLevel = archivistLogLevel;
            InitialTestTokens = initialTestTokens;
            MinPrice = minPrice;
            MaxCollateral = maxCollateral;
            MaxDuration = maxDuration;
            BlockTTL = blockTTL;
            BlockMI = blockMI;
            BlockMN = blockMN;
        }

        public string Name { get; }
        public DateTime StartUtc { get; }
        public DateTime FinishedUtc { get; }
        public string KubeNamespace { get; }
        public int NumberOfArchivistNodes { get; }
        public int NumberOfValidators { get; }
        public int StorageQuotaMB { get; }
        public ArchivistLogLevel ArchivistLogLevel { get; }
        public int InitialTestTokens { get; }
        public int MinPrice { get; }
        public int MaxCollateral { get; }
        public int MaxDuration { get; }
        public int BlockTTL { get; }
        public int BlockMI { get; }
        public int BlockMN { get; }
    }
}
