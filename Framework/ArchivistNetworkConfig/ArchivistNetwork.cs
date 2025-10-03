namespace ArchivistNetworkConfig
{
    public class ArchivistNetwork
    {
        public ArchivistVersionEntry Version { get; set; } = new ArchivistVersionEntry();
        public ArchivistSprEntry SPR { get; set; } = new ArchivistSprEntry();
        public string[] RPCs { get; set; } = Array.Empty<string>();
        public ArchivistMarketplaceEntry Marketplace { get; set; } = new ArchivistMarketplaceEntry();
        public TeamObject Team { get; set; } = new TeamObject();
    }

    public class TeamObject
    {
        public TeamNodesCategory[] Nodes { get; set; } = Array.Empty<TeamNodesCategory>();
        public ArchivistNetworkTeamUtilsObject Utils { get; set; } = new ArchivistNetworkTeamUtilsObject();

        public Dictionary<string, string> GetNodesAsLogReplacements()
        {
            var result = new Dictionary<string, string>();
            foreach (var node in Nodes)
            {
                foreach (var instance in node.Instances)
                {
                    result.Add(instance.EthAddress, instance.Name);
                }
            }
            return result;
        }
    }

    public class TeamNodesCategory
    {
        public string Category { get; set; } = string.Empty;
        public ArchivistNetworkTeamNodesVersionsInstancesEntry[] Instances { get; set; } = Array.Empty<ArchivistNetworkTeamNodesVersionsInstancesEntry>();
    }
}
