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
        private const string OpenApiYamlHash = "F3-9B-69-4D-04-6D-28-28-34-76-85-92-BC-7A-7C-4E-0B-77-B4-8D-06-4F-41-E2-95-7F-20-71-F8-0F-E9-4A";
        private const string OpenApiFilePath = "/archivist/openapi.yaml";
        private const string DisableEnvironmentVariable = "ARCHIVISTPLUGIN_DISABLE_APICHECK";

        private const string ExpectedSystemTestingOptionsWarning = "This application was compiled with system testing options enabled.";

        private const bool Disable = false;

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


            if (!IsContainerApiCompatible(workflow, container)) Fail();
            if (!IsCompiledWithSystemTestingOptions(workflow, container)) Fail();

            checkPassed = true;
        }

        private bool IsCompiledWithSystemTestingOptions(IStartupWorkflow workflow, RunningContainer container)
        {
            var log = workflow.DownloadContainerLog(container);
            var lines = log.GetLinesContaining(ExpectedSystemTestingOptionsWarning);
            if (lines.Any())
            {
                Log("System testing options confirmed.");
                return true;
            }

            Log("Application was not compiled with system testing options.");
            return false;
        }

        private bool IsContainerApiCompatible(IStartupWorkflow workflow, RunningContainer container)
        {
            var containerApi = workflow.ExecuteCommand(container, "cat", OpenApiFilePath);
            if (string.IsNullOrEmpty(containerApi)) return false;

            var containerHash = Hash(containerApi);
            if (containerHash != OpenApiYamlHash)
            {
                OverwriteOpenApiYaml(containerApi);
                return false;
            }

            Log("Container API compatibility check passed.");
            return true;
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
