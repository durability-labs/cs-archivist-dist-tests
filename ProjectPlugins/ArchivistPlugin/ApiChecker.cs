using Core;
using KubernetesWorkflow;
using KubernetesWorkflow.Types;
using Logging;
using System.Security.Cryptography;
using System.Text;
using Utils;

namespace ArchivistPlugin
{
    public class ApiChecker
    {
        // <INSERT-OPENAPI-YAML-HASH>
        private const string OpenApiYamlHash = "A4-32-F1-C6-C5-20-E9-9D-FF-9D-C4-BA-53-FA-13-AC-F3-7C-B7-B4-51-E5-B8-60-0F-C2-40-E2-CF-E5-60-F9";
        private const string OpenApiFilePath = "/archivist/openapi.yaml";
        private const string DisableEnvironmentVariable = "ARCHIVISTPLUGIN_DISABLE_APICHECK";

        private const bool Disable = false;

        private const string Warning =
            "Warning: ArchivistPlugin was unable to find the openapi.yaml file in the Archivist container. Are you running an old version of Archivist? " +
            "Plugin will continue as normal, but API compatibility is not guaranteed!";

        private const string Failure =
            "Archivist API compatibility check failed! " +
            "openapi.yaml used by ArchivistPlugin does not match openapi.yaml in Archivist container. The openapi.yaml in " +
            "'ProjectPlugins/ArchivistPlugin' has been overwritten with the container one. " +
            "Please and rebuild this project. If you wish to disable API compatibility checking, please set " +
            $"the environment variable '{DisableEnvironmentVariable}' or set the disable bool in 'ProjectPlugins/ArchivistPlugin/ApiChecker.cs'.";

        private static bool checkPassed = false;

        private readonly IPluginTools pluginTools;
        private readonly ILog log;

        public ApiChecker(IPluginTools pluginTools)
        {
            this.pluginTools = pluginTools;
            log = pluginTools.GetLog();

            if (string.IsNullOrEmpty(OpenApiYamlHash)) throw new Exception("OpenAPI yaml hash was not inserted by pre-build trigger.");
        }

        public void CheckCompatibility(RunningPod[] containers)
        {
            if (checkPassed) return;

            Log("ArchivistPlugin is checking API compatibility...");

            if (Disable || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(DisableEnvironmentVariable)))
            {
                Log("API compatibility checking has been disabled.");
                checkPassed = true;
                return;
            }

            var workflow = pluginTools.CreateWorkflow();
            var container = containers.First().Containers.First();
            var containerApi = workflow.ExecuteCommand(container, "cat", OpenApiFilePath);

            if (string.IsNullOrEmpty(containerApi))
            {
                log.Error(Warning);
                Fail();
            }

            if (!CheckSystemTestingOptionsEnabled(container, workflow))
            {
                log.Error("Application is not built with system testing options enabled.");
                Fail();
            }

            var containerHash = Hash(containerApi);
            if (containerHash == OpenApiYamlHash)
            {
                Log("API compatibility check passed.");
                checkPassed = true;
                return;
            }

            OverwriteOpenApiYaml(containerApi);

            Fail();
        }

        private bool CheckSystemTestingOptionsEnabled(RunningContainer container, IStartupWorkflow workflow)
        {
            var lines = workflow.DownloadContainerLog(container);
            return lines.GetLinesContaining("This application was compiled with system testing options enabled").Any();
        }

        private void Fail()
        {
            log.Error(Failure);
            throw new Exception(Failure);
        }

        private void OverwriteOpenApiYaml(string containerApi)
        {
            Log("API compatibility check failed. Updating ArchivistPlugin...");
            var openApiFilePath = Path.Combine(PluginPathUtils.ProjectPluginsDir, "ArchivistClient", "openapi.yaml");
            if (!File.Exists(openApiFilePath)) throw new Exception("Unable to locate ArchivistClient/openapi.yaml. Expected: " + openApiFilePath);

            File.Delete(openApiFilePath);
            File.WriteAllText(openApiFilePath, containerApi);
            Log("ArchivistClient/openapi.yaml has been updated.");
        }

        private string Hash(string file)
        {
            var fileBytes = Encoding.ASCII.GetBytes(file
                .Replace(Environment.NewLine, ""));
            var sha = SHA256.Create();
            var hash = sha.ComputeHash(fileBytes);
            return BitConverter.ToString(hash);
        }

        private void Log(string msg)
        {
            log.Log(msg);
        }
    }
}
