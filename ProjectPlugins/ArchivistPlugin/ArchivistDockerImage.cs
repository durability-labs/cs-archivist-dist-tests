namespace ArchivistPlugin
{
    public class ArchivistDockerImage
    {
        private const string DefaultDockerImage =
            //"durabilitylabs/archivist-node:sha-1ad57bf-dist-tests"; // overlay - stores encoded manifest twice somehow
            "durabilitylabs/archivist-node:sha-7e8c0c8-dist-tests"; // main - Feat/make submodules great again(#96) - correct no duplicates

        public string GetArchivistDockerImage()
        {
            var image = Environment.GetEnvironmentVariable("ARCHIVISTDOCKERIMAGE");
            if (!string.IsNullOrEmpty(image)) return image;
            return DefaultDockerImage;
        }
    }
}
