namespace ArchivistDiscordBotPlugin
{
    public class DiscordBotStartupConfig
    {
        public DiscordBotStartupConfig(string name, string token, string serverName, string adminRoleName, string adminChannelName, string kubeNamespace, DiscordBotGethInfo gethInfo, string rewardChannelName)
        {
            Name = name;
            Token = token;
            ServerName = serverName;
            AdminRoleName = adminRoleName;
            AdminChannelName = adminChannelName;
            KubeNamespace = kubeNamespace;
            GethInfo = gethInfo;
            RewardChannelName = rewardChannelName;
        }

        public string Name { get; }
        public string Token { get; }
        public string ServerName { get; }
        public string AdminRoleName { get; }
        public string AdminChannelName { get; }
        public string RewardChannelName { get; }
        public string KubeNamespace { get; }
        public DiscordBotGethInfo GethInfo { get; }
        public string? DataPath { get; set; }
    }

    public class RewarderBotStartupConfig
    {
        public RewarderBotStartupConfig(string name, string discordBotHost, int discordBotPort, int intervalMinutes, DateTime historyStartUtc, DiscordBotGethInfo gethInfo, string? dataPath)
        {
            Name = name;
            DiscordBotHost = discordBotHost;
            DiscordBotPort = discordBotPort;
            IntervalMinutes = intervalMinutes;
            HistoryStartUtc = historyStartUtc;
            GethInfo = gethInfo;
            DataPath = dataPath;
        }

        public string Name { get; }
        public string DiscordBotHost { get; }
        public int DiscordBotPort { get; }
        public int IntervalMinutes { get; }
        public DateTime HistoryStartUtc { get; }
        public DiscordBotGethInfo GethInfo { get; }
        public string? DataPath { get; set; }
    }

    public class DiscordBotGethInfo
    {
        public DiscordBotGethInfo(string host, int port, string privKey, string marketplaceAddress, string tokenAddress, string abi)
        {
            Host = host;
            Port = port;
            PrivKey = privKey;
            MarketplaceAddress = marketplaceAddress;
            TokenAddress = tokenAddress;
            Abi = abi;
        }

        public string Host { get; }
        public int Port { get; }
        public string PrivKey { get; }
        public string MarketplaceAddress { get; }
        public string TokenAddress { get; }
        public string Abi { get; }
    }
}
