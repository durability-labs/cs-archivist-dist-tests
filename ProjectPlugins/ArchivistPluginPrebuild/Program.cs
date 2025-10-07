using System.Security.Cryptography;
using System.Text;
using Utils;

public static class Program
{
    private const string Search = "<INSERT-OPENAPI-YAML-HASH>";
    private const string ArchivistPluginFolderName = "ArchivistPlugin";
    private const string ProjectPluginsFolderName = "ProjectPlugins";

    public static void Main(string[] args)
    {
        Console.WriteLine("Injecting hash of 'openapi.yaml'...");

        var pluginRoot = FindArchivistPluginFolder();
        var clientRoot = FindArchivistClientFolder();
        Console.WriteLine("Located ArchivistPlugin: " + pluginRoot);
        Console.WriteLine("Located ArchivistClient: " + clientRoot);
        var openApiFile = Path.Combine(clientRoot, "openapi.yaml");
        var clientFile = Path.Combine(clientRoot, "obj", "openapiClient.cs");
        var targetFile = Path.Combine(pluginRoot, "ApiChecker.cs");

        // Force client rebuild by deleting previous artifact.
        File.Delete(clientFile);

        var hash = CreateHash(openApiFile);
        // This hash is used to verify that the Archivist docker image being used is compatible
        // with the openapi.yaml being used by the Archivist plugin.
        // If the openapi.yaml files don't match, an exception is thrown.

        SearchAndInject(hash, targetFile);

        // This program runs as the pre-build trigger for "ArchivistPlugin".
        // You might be wondering why this work isn't done by a shell script.
        // This is because this project is being run on many different platforms.
        // (Mac, Unix, Win, but also desktop/cloud containers.)
        // In order to not go insane trying to make a shell script that works in all possible cases,
        // instead we use the one tool that's definitely installed in all platforms and locations
        // when you're trying to run this plugin.

        Console.WriteLine("Done!");
    }

    private static string FindArchivistPluginFolder()
    {
        var folder = Path.Combine(PluginPathUtils.ProjectPluginsDir, "ArchivistPlugin");
        if (!Directory.Exists(folder)) throw new Exception("ArchivistPlugin folder not found. Expected: " + folder);
        return folder;
    }

    private static string FindArchivistClientFolder()
    {
        var folder = Path.Combine(PluginPathUtils.ProjectPluginsDir, "ArchivistClient");
        if (!Directory.Exists(folder)) throw new Exception("ArchivistClient folder not found. Expected: " + folder);
        return folder;
    }

    private static string CreateHash(string openApiFile)
    {
        var file = File.ReadAllText(openApiFile);
        var fileBytes = Encoding.ASCII.GetBytes(file
            .Replace(Environment.NewLine, ""));

        var sha = SHA256.Create();
        var hash = sha.ComputeHash(fileBytes);
        return BitConverter.ToString(hash);
    }

    private static void SearchAndInject(string hash, string targetFile)
    {
        var lines = File.ReadAllLines(targetFile);
        Inject(lines, hash);
        File.WriteAllLines(targetFile, lines);
    }

    private static void Inject(string[] lines, string hash)
    {
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains(Search))
            {
                lines[i + 1] = $"        private const string OpenApiYamlHash = \"{hash}\";";
                return;
            }
        }
    }
}
