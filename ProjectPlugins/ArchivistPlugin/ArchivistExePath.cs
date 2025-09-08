namespace ArchivistPlugin
{
    public class ArchivistExePath
    {
        private readonly string[] paths = [
            Path.Combine("d:", "Dev", "archivist-node", "build", "archivist.exe"),
            Path.Combine("c:", "Projects", "archivist-node", "build", "archivist.exe")
        ];

        private string selectedPath = string.Empty;

        public ArchivistExePath()
        {
            foreach (var p in paths)
            {
                if (File.Exists(p))
                {
                    selectedPath = p;
                    return;
                }
            }
        }

        public string Get()
        {
            return selectedPath;
        }
    }
}
