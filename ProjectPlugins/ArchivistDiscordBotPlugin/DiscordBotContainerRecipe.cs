using KubernetesWorkflow;
using KubernetesWorkflow.Recipe;
using Utils;

namespace ArchivistDiscordBotPlugin
{
    public class DiscordBotContainerRecipe : ContainerRecipeFactory
    {
        public override string AppName => "discordbot-bibliotech";
        public override string Image => "durabilitylabs/archivist-discordbot:sha-f5ae024";

        public static string RewardsPort = "bot_rewards_port";

        protected override void Initialize(StartupConfig startupConfig)
        {
            var config = startupConfig.Get<DiscordBotStartupConfig>();

            SetSchedulingAffinity(notIn: "false");

            AddEnvVar("TOKEN", config.Token);
            AddEnvVar("SERVERNAME", config.ServerName);
            AddEnvVar("ADMINROLE", config.AdminRoleName);
            AddEnvVar("ADMINCHANNELNAME", config.AdminChannelName);
            AddEnvVar("REWARDSCHANNELNAME", config.RewardChannelName);
            AddEnvVar("KUBECONFIG", "/opt/kubeconfig.yaml");
            AddEnvVar("KUBENAMESPACE", config.KubeNamespace);

            var gethInfo = config.GethInfo;
            AddEnvVar("GETH_HOST", gethInfo.Host);
            AddEnvVar("GETH_HTTP_PORT", gethInfo.Port.ToString());
            AddEnvVar("GETH_PRIVATE_KEY", gethInfo.PrivKey);
            AddEnvVar("ARCHIVISTCONTRACTS_MARKETPLACEADDRESS", gethInfo.MarketplaceAddress);
            AddEnvVar("ARCHIVISTCONTRACTS_ABI", gethInfo.Abi);

            AddEnvVar("NODISCORD", "1");

            AddInternalPortAndVar("REWARDAPIPORT", RewardsPort);

            if (!string.IsNullOrEmpty(config.DataPath))
            {
                AddEnvVar("DATAPATH", config.DataPath);
                AddVolume(config.DataPath, 1.GB());
            }
        }
    }
}
