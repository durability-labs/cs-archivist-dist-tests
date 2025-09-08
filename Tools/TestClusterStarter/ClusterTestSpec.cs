using KubernetesWorkflow.Types;

namespace TestClusterStarter
{
    public class ClusterTestSetup
    {
        public ClusterTestSetup(ClusterTestSpec[] specs)
        {
            Specs = specs;
        }

        public ClusterTestSpec[] Specs { get; }
    }

    public class ClusterTestSpec
    {
        public ClusterTestSpec(string name, string filter, int replication, int durationSeconds, string? archivistImageOverride)
        {
            Name = name;
            Filter = filter;
            Replication = replication;
            DurationSeconds = durationSeconds;
            ArchivistImageOverride = archivistImageOverride;
        }

        public string Name { get; }
        public string Filter { get; }
        public int Replication { get; }
        public int DurationSeconds { get; }
        public string? ArchivistImageOverride { get; }
    }

    public class ClusterTestDeployment
    {
        public ClusterTestDeployment(RunningContainer[] containers)
        {
            Containers = containers;
        }

        public RunningContainer[] Containers { get; }
    }
}
