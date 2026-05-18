using Utils;

namespace ArchivistPlugin
{
    public class ArchivistDockerImage
    {
        private const string DefaultDockerImage =
            "durabilitylabs/archivist-node:latest-dist-tests";

        public string GetArchivistDockerImage()
        {
            return EnvVar.GetOrDefault("ARCHIVISTDOCKERIMAGE", DefaultDockerImage);
        }
    }
}
