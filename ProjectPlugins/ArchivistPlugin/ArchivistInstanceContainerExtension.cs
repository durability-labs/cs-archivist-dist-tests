using ArchivistClient;
using KubernetesWorkflow.Types;
using Utils;

namespace ArchivistPlugin
{
    public static class ArchivistInstanceContainerExtension
    {
        public static IArchivistInstance CreateFromPod(RunningPod pod)
        {
            var container = pod.Containers.Single();

            return new ArchivistInstance(
                name: container.Name,
                imageName: container.Recipe.Image,
                startUtc: container.Recipe.RecipeCreatedUtc,
                discoveryEndpoint: SetClusterInternalIpAddress(pod, container.GetInternalAddress(ArchivistContainerRecipe.DiscoveryPortTag)),
                apiEndpoint: container.GetAddress(ArchivistContainerRecipe.ApiPortTag),
                listenEndpoint: container.GetInternalAddress(ArchivistContainerRecipe.ListenPortTag),
                ethAccount: container.Recipe.Additionals.Get<EthAccount>(),
                metricsEndpoint: GetMetricsEndpoint(container)
            );
        }

        private static Address SetClusterInternalIpAddress(RunningPod pod, Address address)
        {
            return new Address(
                logName: address.LogName,
                host: pod.PodInfo.Ip,
                port: address.Port
            );
        }

        private static Address? GetMetricsEndpoint(RunningContainer container)
        {
            try
            {
                return container.GetInternalAddress(ArchivistContainerRecipe.MetricsPortTag);
            }
            catch
            {
                return null;
            }
        }
    }
}
