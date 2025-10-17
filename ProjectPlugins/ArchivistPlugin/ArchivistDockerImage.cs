namespace ArchivistPlugin
{
    public class ArchivistDockerImage
    {
        private const string DefaultDockerImage =
            //"durabilitylabs/archivist-node:latest-dist-tests";
            "durabilitylabs/archivist-node:sha-eebb86b-dist-tests";

        get new image: https://github.com/durability-labs/archivist-node/actions/runs/18594188978

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
