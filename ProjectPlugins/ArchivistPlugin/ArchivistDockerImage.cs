namespace ArchivistPlugin
{
    public class ArchivistDockerImage
    {
        private const string DefaultDockerImage = "durabilitylabs/archivist-node:latest-dist-tests";

        public string GetArchivistDockerImage()
        {
            var image = Environment.GetEnvironmentVariable("ARCHIVISTDOCKERIMAGE");
            if (!string.IsNullOrEmpty(image)) return image;
            return DefaultDockerImage;
        }
    }
}
