namespace ArchivistPlugin
{
    public class ArchivistDockerImage
    {
        private const string DefaultDockerImage = "durabilitylabs/archivist-node:latest-dist-tests";

        public static string Override { get; set; } = string.Empty;

        public string GetArchivistDockerImage()
        {
            var image = Environment.GetEnvironmentVariable("ARCHIVISTDOCKERIMAGE");
            if (!string.IsNullOrEmpty(image)) return image;
            if (!string.IsNullOrEmpty(Override)) return Override;
            return DefaultDockerImage;
        }
    }
}
