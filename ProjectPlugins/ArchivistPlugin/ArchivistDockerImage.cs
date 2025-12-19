namespace ArchivistPlugin
{
    public class ArchivistDockerImage
    {
        private const string DefaultDockerImage =
            //"durabilitylabs/archivist-node:latest-dist-tests";
            "durabilitylabs/archivist-node:sha-3b985bd-dist-tests";

        public string GetArchivistDockerImage()
        {
            var image = Environment.GetEnvironmentVariable("ARCHIVISTDOCKERIMAGE");
            if (!string.IsNullOrEmpty(image)) return image;
            return DefaultDockerImage;
        }
    }
}
