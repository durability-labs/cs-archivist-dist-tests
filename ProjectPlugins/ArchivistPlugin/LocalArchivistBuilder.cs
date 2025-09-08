using ArchivistPlugin;
using Logging;
using System.Diagnostics;

namespace ArchivistNetDeployer
{
    public class LocalArchivistBuilder
    {
        private readonly ILog log;
        private readonly string? repoPath;
        private readonly string? dockerUsername;

        public LocalArchivistBuilder(ILog log, string? repoPath, string? dockerUsername)
        {
            this.log = new LogPrefixer(log, "(LocalArchivistBuilder) ");
            this.repoPath = repoPath;
            this.dockerUsername = dockerUsername;
        }

        public LocalArchivistBuilder(ILog log, string? repoPath)
            : this(log, repoPath, Environment.GetEnvironmentVariable("DOCKERUSERNAME"))
        {
        }

        public LocalArchivistBuilder(ILog log)
            : this(log, Environment.GetEnvironmentVariable("ARCHIVISTREPOPATH"))
        {
        }

        public void Intialize()
        {
            if (!IsEnabled()) return;

            if (string.IsNullOrEmpty(dockerUsername)) throw new Exception("Docker username required. (Pass to constructor or set 'DOCKERUSERNAME' environment variable.)");
            if (string.IsNullOrEmpty(repoPath)) throw new Exception("Archivist repo path required. (Pass to constructor or set 'ARCHIVISTREPOPATH' environment variable.)");
            if (!Directory.Exists(repoPath)) throw new Exception($"Path '{repoPath}' does not exist.");
            var files = Directory.GetFiles(repoPath);
            if (!files.Any(f => f.ToLowerInvariant().EndsWith("archivist.nim"))) throw new Exception($"Path '{repoPath}' does not appear to be the Archivist repo root.");

            Log($"Archivist docker image will be built in path '{repoPath}'.");
            Log("Please note this can take several minutes. If you're not trying to use a Archivist image with local code changes,");
            Log("Consider using the default test image or consider setting the 'ARCHIVISTDOCKERIMAGE' environment variable to use an already built image.");
            ArchivistDockerImage.Override = $"Using docker image locally built in path '{repoPath}'.";
        }

        public void Build()
        {
            if (!IsEnabled()) return;
            Log("Docker login...");
            DockerLogin();

            Log($"Logged in. Building Archivist image in path '{repoPath}'...");

            var customImage = GenerateImageName();
            Docker($"build", "-t", customImage, "-f", "./archivist.Dockerfile",
                "--build-arg=\"MAKE_PARALLEL=4\"",
                "--build-arg=\"NIMFLAGS=-d:disableMarchNative -d:archivist_enable_api_debug_peers=true -d:archivist_enable_api_debug_fetch=true -d:archivist_enable_simulated_proof_failures\"",
                "--build-arg=\"NAT_IP_AUTO=true\"",
                "..");

            Log($"Image '{customImage}' built successfully. Pushing...");

            Docker("push", customImage);

            ArchivistDockerImage.Override = customImage;
            Log("Image pushed. Good to go!");
        }

        private void DockerLogin()
        {
            var dockerPassword = Environment.GetEnvironmentVariable("DOCKERPASSWORD");

            try
            {
                if (string.IsNullOrEmpty(dockerUsername) || string.IsNullOrEmpty(dockerPassword))
                {
                    Log("Environment variable 'DOCKERPASSWORD' not provided.");
                    Log("Trying system default...");
                    Docker("login");
                }
                else
                {
                    Docker("login", "-u", dockerUsername, "-p", dockerPassword);
                }
            }
            catch
            {
                Log("Docker login failed.");
                Log("Please check the docker username and password provided by the constructor arguments and/or");
                Log("set by 'DOCKERUSERNAME' and 'DOCKERPASSWORD' environment variables.");
                Log("Note: You can use a docker access token as DOCKERPASSWORD.");
                throw;
            }
        }

        private string GenerateImageName()
        {
            var tag = Environment.GetEnvironmentVariable("DOCKERTAG");
            if (string.IsNullOrEmpty(tag)) return $"{dockerUsername!}/archivist-node-autoimage:{Guid.NewGuid().ToString().ToLowerInvariant()}";
            return $"{dockerUsername}/archivist-node-autoimage:{tag}";
        }

        private void Docker(params string[] args)
        {
            var dockerPath = Path.Combine(repoPath!, "docker");

            var startInfo = new ProcessStartInfo()
            {
                FileName = "docker",
                Arguments = string.Join(" ", args),
                WorkingDirectory = dockerPath,
            };
            var process = Process.Start(startInfo);
            if (process == null) throw new Exception("Failed to start docker process.");
            if (!process.WaitForExit(TimeSpan.FromMinutes(10))) throw new Exception("Docker processed timed out after 10 minutes.");
            if (process.ExitCode != 0) throw new Exception("Docker process exited with error.");
        }

        private bool IsEnabled()
        {
            return !string.IsNullOrEmpty(repoPath);
        }

        private void Log(string msg)
        {
            log.Log(msg);
        }
    }
}
