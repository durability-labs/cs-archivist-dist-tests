namespace ArchivistNetworkConfig
{
    [Serializable]
    public class NetworkConfig
    {
        public string Latest { get; set; } = string.Empty;
        public ArchivistVersionEntry[] Archivist { get; set; } = Array.Empty<ArchivistVersionEntry>();
        public ArchivistSprEntry[] SPRs { get; set; } = Array.Empty<ArchivistSprEntry>();
        public string[] RPCs { get; set; } = Array.Empty<string>();
        public ArchivistMarketplaceEntry[] Marketplace { get; set; } = Array.Empty<ArchivistMarketplaceEntry>();
        public ArchivistNetworkTeamObject Team { get; set; } = new ArchivistNetworkTeamObject();
    }

    [Serializable]
    public class ArchivistVersionEntry
    {
        public string Version { get; set; } = string.Empty;
        public string Revision { get; set; } = string.Empty;
        public string Contracts { get; set; } = string.Empty;
    }

    [Serializable]
    public class ArchivistSprEntry
    {
        public string[] SupportedVersions { get; set; } = Array.Empty<string>();
        public string[] Records { get; set; } = Array.Empty<string>();
    }

    [Serializable]
    public class ArchivistMarketplaceEntry
    {
        public string[] SupportedVersions { get; set; } = Array.Empty<string>();
        public string ContractAddress { get; set; } = string.Empty;
        public string ABI { get; set; } = string.Empty;
    }

    [Serializable]
    public class ArchivistNetworkTeamObject
    {
        public ArchivistNetworkTeamNodesEntry[] Nodes { get; set; } = Array.Empty<ArchivistNetworkTeamNodesEntry>();
        public ArchivistNetworkTeamUtilsObject Utils { get; set; } = new ArchivistNetworkTeamUtilsObject();
    }

    [Serializable]
    public class ArchivistNetworkTeamNodesEntry
    {
        public string Category { get; set; } = string.Empty;
        public ArchivistNetworkTeamNodesVersionsEntry[] Versions { get; set; } = Array.Empty<ArchivistNetworkTeamNodesVersionsEntry>();
    }

    [Serializable]
    public class ArchivistNetworkTeamNodesVersionsEntry
    {
        public string Version { get; set; } = string.Empty;
        public ArchivistNetworkTeamNodesVersionsInstancesEntry[] Instances { get; set; } = Array.Empty<ArchivistNetworkTeamNodesVersionsInstancesEntry>();
    }

    [Serializable]
    public class ArchivistNetworkTeamNodesVersionsInstancesEntry
    {
        public string Name { get; set; } = string.Empty;
        public string PodName { get; set; } = string.Empty;
        public string EthAddress { get; set; } = string.Empty;
    }

    [Serializable]
    public class ArchivistNetworkTeamUtilsObject
    {
        public string CrawlerRpc { get; set; } = string.Empty;
        public string BotRpc { get; set; } = string.Empty;
        public string ElasticSearch { get; set; } = string.Empty;
        public string Gateway { get; set; } = string.Empty;
        public string TransactionLinkFormat { get; set; } = string.Empty;
    }
}
