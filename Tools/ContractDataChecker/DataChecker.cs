using ArchivistClient;
using ArchivistContractsPlugin.ChainMonitor;
using Logging;
using TestNetRewarder;
using Utils;

namespace ContractDataChecker
{
    public class DataChecker : IChainFollowingHooks
    {
        private readonly LogPrefixer log;
        private readonly IArchivistNode archivistNode;
        private ChainState chainState = null!;

        public DataChecker(LogPrefixer log, IArchivistNode archivistNode)
        {
            this.log = log;
            this.archivistNode = archivistNode;
        }

        private void CheckRandomRequestData()
        {
            if (chainState == null) return;

            var runningRequests = chainState.Requests.Where(r => r.State == ArchivistContractsPlugin.RequestState.Started).ToArray();
            if (runningRequests.Length == 0)
            {
                Log("No running requests known.");
                return;
            }
            var request = RandomUtils.GetOneRandom(runningRequests);

            var manifest = FetchManifest(request);
            if (manifest == null)
            {
                Log("Failed to fetch manifest.");
                return;
            }
            if (manifest.Manifest.DatasetSize.SizeInBytes > 100.MB().SizeInBytes)
            {
                Log($"Fetched manifest but dataset is too large to try download: {manifest.Manifest.DatasetSize}");
                return;
            }
            if (!SuccessfulDownload(request))
            {
                Log("Failed to download.");
                return;
            }
            Log("Successfully downloaded contract data.");
        }

        private bool SuccessfulDownload(IChainStateRequest request)
        {
            try
            {
                var result = archivistNode.DownloadContent(request.Cid);
                if (result != null)
                {
                    try
                    {
                        File.Delete(result.Filename);
                    }
                    catch { Log("Failed to delete downloaded file!"); }
                }
                return result != null;
            }
            catch { }
            return false;
        }

        private LocalDataset? FetchManifest(IChainStateRequest request)
        {
            try
            {
                return archivistNode.DownloadManifestOnly(request.Cid);
            }
            catch { }
            return null;
        }

        private void Log(string v)
        {
            log.Log(v);
        }

        #region Hooks

        public void OnError(string msg)
        {
        }

        public async Task OnInitialized(ChainState chainState, int recoveredRequests)
        {
            this.chainState = chainState;
        }

        public async Task OnLoopStepFinished()
        {
            try
            {
                CheckRandomRequestData();
            }
            catch (Exception ex)
            {
                log.Error($"{nameof(CheckRandomRequestData)} = {ex}");
            }
        }

        public async Task OnLoopStepStarting()
        {
        }

        public async Task OnRunStarting()
        {
        }

        #endregion
    }
}
