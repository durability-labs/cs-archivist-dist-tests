using Core;
using KubernetesWorkflow.Types;

namespace ArchivistDiscordBotPlugin
{
    public static class CoreInterfaceExtensions
    {
        public static RunningPod DeployArchivistDiscordBot(this CoreInterface ci, DiscordBotStartupConfig config)
        {
            return Plugin(ci).Deploy(config);
        }

        public static RunningPod DeployRewarderBot(this CoreInterface ci, RewarderBotStartupConfig config)
        {
            return Plugin(ci).DeployRewarder(config);
        }

        private static ArchivistDiscordBotPlugin Plugin(CoreInterface ci)
        {
            return ci.GetPlugin<ArchivistDiscordBotPlugin>();
        }
    }
}
