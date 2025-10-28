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
        private const string OpenApiYamlHash = "9B-59-29-C7-59-A0-7F-F6-73-E3-89-11-4B-7F-42-93-58-0A-F3-26-3D-15-25-7A-08-A3-10-C7-C9-58-AE-38";
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
