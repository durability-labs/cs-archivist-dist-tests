using Utils;

namespace ArchivistPlugin
{
    public class ArchivistDockerImage
    {
        private const string DefaultDockerImage =
            //"durabilitylabs/archivist-node:sha-314a2c7-dist-tests";
            "durabilitylabs/archivist-node:sha-ea52406-dist-tests"; // one slot worker

        public string GetArchivistDockerImage()
        {
            return EnvVar.GetOrDefault("ARCHIVISTDOCKERIMAGE", DefaultDockerImage);
        }
    }
}
