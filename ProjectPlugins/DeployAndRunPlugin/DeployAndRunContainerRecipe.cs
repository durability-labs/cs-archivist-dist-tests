using KubernetesWorkflow;
using KubernetesWorkflow.Recipe;

namespace DeployAndRunPlugin
{
    public class DeployAndRunContainerRecipe : ContainerRecipeFactory
    {
        public override string AppName => "deploy-and-run";
        public override string Image => "thatbenbierens/dist-tests-deployandrun:initial";

        protected override void Initialize(StartupConfig config)
        {
            var setup = config.Get<RunConfig>();

            if (setup.ArchivistImageOverride != null)
            {
                AddEnvVar("ARCHIVISTDOCKERIMAGE", setup.ArchivistImageOverride);
            }

            AddEnvVar("DNR_REP", setup.Replications.ToString());
            AddEnvVar("DNR_NAME", setup.Name);
            AddEnvVar("DNR_FILTER", setup.Filter);
            AddEnvVar("DNR_DURATION", setup.Duration.TotalSeconds.ToString());

            AddEnvVar("KUBECONFIG", "/opt/kubeconfig.yaml");
            AddEnvVar("LOGPATH", "/var/log/archivist-continuous-tests");

            AddVolume(name: "kubeconfig", mountPath: "/opt/kubeconfig.yaml", subPath: "kubeconfig.yaml", secret: "archivist-dist-tests-app-kubeconfig");
            AddVolume(name: "logs", mountPath: "/var/log/archivist-continuous-tests", hostPath: "/var/log/archivist-continuous-tests");
        }
    }

    public class RunConfig
    {
        public RunConfig(string name, string filter, TimeSpan duration, int replications, string? archivistImageOverride = null)
        {
            Name = name;
            Filter = filter;
            Duration = duration;
            Replications = replications;
            ArchivistImageOverride = archivistImageOverride;
        }

        public string Name { get; }
        public string Filter { get; }
        public TimeSpan Duration { get; }
        public int Replications { get; }
        public string? ArchivistImageOverride { get; }
    }
}
