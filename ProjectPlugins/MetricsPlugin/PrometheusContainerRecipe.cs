using Core;
using KubernetesWorkflow;
using KubernetesWorkflow.Recipe;

namespace MetricsPlugin
{
    public class PrometheusContainerRecipe : ContainerRecipeFactory
    {
        private const string DockerImageEnvVar = "PROMETHEUS_DOCKER_IMAGE";
        private const string ImagePullPolicyEnvVar = "PROMETHEUS_IMAGE_PULL_POLICY";
        private const string DefaultDockerImage = "durabilitylabs/dist-tests-prometheus:latest";

        public override string AppName => "prometheus";
        public override string Image => EnvironmentVariables.GetStringOrDefault(DockerImageEnvVar, DefaultDockerImage);
        public override string? ImagePullPolicy => EnvironmentVariables.GetNullableStringOrDefault(ImagePullPolicyEnvVar);

        public const string PortTag = "prometheus_port_tag";

        protected override void Initialize(StartupConfig startupConfig)
        {
            var config = startupConfig.Get<PrometheusStartupConfig>();

            SetSchedulingAffinity(notIn: "false");

            AddExposedPortAndVar("PROM_PORT", PortTag);
            AddEnvVar("PROM_CONFIG", config.PrometheusConfigBase64);
        }
    }
}
