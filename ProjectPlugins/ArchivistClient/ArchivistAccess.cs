using ArchivistOpenApi;
using Logging;
using Newtonsoft.Json;
using Utils;
using WebUtils;

namespace ArchivistClient
{
    public class ArchivistAccess
    {
        private readonly ILog log;
        private readonly IHttpFactory httpFactory;
        private readonly IProcessControl processControl;
        private readonly IArchivistInstance instance;
        private readonly Mapper mapper = new Mapper();

        public ArchivistAccess(ILog log, IHttpFactory httpFactory, IProcessControl processControl, IArchivistInstance instance)
        {
            this.log = log;
            this.httpFactory = httpFactory;
            this.processControl = processControl;
            this.instance = instance;
        }

        public void Stop(bool waitTillStopped)
        {
            processControl.Stop(waitTillStopped);
        }

        public IDownloadedLog DownloadLog(string additionalName = "")
        {
            var file = log.CreateSubfile(GetName() + additionalName);
            Log($"Downloading logs to '{file.Filename}'");
            return processControl.DownloadLog(file);
        }

        public string GetImageName()
        {
            return instance.ImageName;
        }

        public DateTime GetStartUtc()
        {
            return instance.StartUtc;
        }

        public DebugInfo GetDebugInfo()
        {
            return mapper.Map(OnArchivist(api => api.GetDebugInfoAsync()));
        }

        public void SetLogLevel(string logLevel)
        {
            try
            {
                OnArchivist(async api =>
                {
                    await api.SetDebugLogLevelAsync(logLevel);
                    return string.Empty;
                });
            }
            catch (Exception exc)
            {
                log.Error("Failed to set log level: " + exc);
            }
        }

        public string GetSpr()
        {
            return CrashCheck(() =>
            {
                var endpoint = GetEndpoint();
                var json = endpoint.HttpGetString("spr");
                var response = JsonConvert.DeserializeObject<SprResponse>(json);
                return response!.Spr;
            });
        }

        private class SprResponse
        {
            public string Spr { get; set; } = string.Empty;
        }

        public DebugPeer GetDebugPeer(string peerId)
        {
            // Cannot use openAPI: debug/peer endpoint is not specified there.
            return CrashCheck(() =>
            {
                var endpoint = GetEndpoint();
                var str = endpoint.HttpGetString($"debug/peer/{peerId}");

                if (str.ToLowerInvariant() == "unable to find peer!")
                {
                    return new DebugPeer
                    {
                        IsPeerFound = false
                    };
                }

                var result = endpoint.Deserialize<DebugPeer>(str);
                result.IsPeerFound = true;
                return result;
            });
        }

        public void ConnectToPeer(string peerId, string[] peerMultiAddresses)
        {
            OnArchivist(api =>
            {
                Time.Wait(api.ConnectPeerAsync(peerId, peerMultiAddresses));
                return Task.FromResult(string.Empty);
            });
        }

        public string UploadFile(UploadInput uploadInput)
        {
            return OnArchivist(api => api.UploadAsync(uploadInput.ContentType, uploadInput.ContentDisposition, uploadInput.FileStream));
        }

        public Stream DownloadFile(string contentId)
        {
            var fileResponse = OnArchivistNoRetry(api => api.DownloadNetworkStreamAsync(contentId));
            if (fileResponse.StatusCode != 200) throw new Exception("Download failed with StatusCode: " + fileResponse.StatusCode);
            return fileResponse.Stream;
        }

        public LocalDataset DownloadStreamless(ContentId cid)
        {
            var response = OnArchivist(api => api.DownloadNetworkAsync(cid.Id));
            return mapper.Map(response);
        }

        public LocalDataset DownloadManifestOnly(ContentId cid)
        {
            var response = OnArchivist(api => api.DownloadNetworkManifestAsync(cid.Id));
            return mapper.Map(response);
        }

        public LocalDatasetList LocalFiles()
        {
            return mapper.Map(OnArchivist(api => api.ListDataAsync()));
        }

        public StorageAvailability SalesAvailability(CreateStorageAvailability request)
        {
            var body = mapper.Map(request);
            var read = OnArchivist(api => api.OfferStorageAsync(body));
            return mapper.Map(read, GetReservations);
        }

        public StorageAvailability[] GetAvailabilities()
        {
            var collection = OnArchivist(api => api.GetAvailabilitiesAsync());
            return mapper.Map(collection, GetReservations);
        }

        public string RequestStorage(StoragePurchaseRequest request)
        {
            var body = mapper.Map(request);
            return OnArchivist(api => api.CreateStorageRequestAsync(request.ContentId.Id, body));
        }

        public ArchivistSpace Space()
        {
            var space = OnArchivist(api => api.SpaceAsync());
            return mapper.Map(space);
        }

        public StoragePurchase? GetPurchaseStatus(string purchaseId)
        {
            var purchase = OnArchivist(api => api.GetPurchaseAsync(purchaseId));
            return mapper.Map(purchase);
        }

        public string GetName()
        {
            return instance.Name;
        }

        public Address GetDiscoveryEndpoint()
        {
            return instance.DiscoveryEndpoint;
        }

        public Address GetApiEndpoint()
        {
            return instance.ApiEndpoint;
        }

        public Address GetListenEndpoint()
        {
            return instance.ListenEndpoint;
        }

        public bool HasCrashed()
        {
            return processControl.HasCrashed();
        }

        public Address? GetMetricsEndpoint()
        {
            return instance.MetricsEndpoint;
        }

        public EthAccount? GetEthAccount()
        {
            return instance.EthAccount;
        }

        public void DeleteDataDirFolder()
        {
            processControl.DeleteDataDirFolder();
        }

        private ICollection<Reservation> GetReservations(string id)
        {
            return OnArchivist(api => api.GetReservationsAsync(id));
        }

        private T OnArchivistNoRetry<T>(Func<ArchivistApiClient, Task<T>> action)
        {
            var timeSet = httpFactory.WebCallTimeSet;
            var noRetry = new Retry(nameof(OnArchivistNoRetry),
                maxTimeout: TimeSpan.FromSeconds(1.0),
                sleepAfterFail: TimeSpan.FromSeconds(2.0),
                onFail: f => { },
                failFast: true);

            var result = httpFactory.CreateHttp(GetHttpId(), h => CheckContainerCrashed()).OnClient(client => CallArchivist(client, action), noRetry);
            return result;
        }

        private T OnArchivist<T>(Func<ArchivistApiClient, Task<T>> action)
        {
            var result = httpFactory.CreateHttp(GetHttpId(), h => CheckContainerCrashed()).OnClient(client => CallArchivist(client, action));
            return result;
        }

        private T CallArchivist<T>(HttpClient client, Func<ArchivistApiClient, Task<T>> action)
        {
            var address = GetAddress();
            var api = new ArchivistApiClient(client);
            api.BaseUrl = $"{address.Host}:{address.Port}/api/archivist/v1";
            return CrashCheck(() => Time.Wait(action(api)));
        }

        private T CrashCheck<T>(Func<T> action)
        {
            try
            {
                return action();
            }
            finally
            {
                CheckContainerCrashed();
            }
        }

        private IEndpoint GetEndpoint()
        {
            return httpFactory
                .CreateHttp(GetHttpId(), h => CheckContainerCrashed())
                .CreateEndpoint(GetAddress(), "/api/archivist/v1/", GetName());
        }

        private Address GetAddress()
        {
            return instance.ApiEndpoint;
        }

        private string GetHttpId()
        {
            return GetAddress().ToString();
        }

        private void CheckContainerCrashed()
        {
            if (processControl.HasCrashed()) throw new Exception($"Container {GetName()} has crashed.");
        }

        private void Throw(Failure failure)
        {
            throw failure.Exception;
        }

        private void Log(string msg)
        {
            log.Log(msg);
        }
    }

    public class UploadInput
    {
        public UploadInput(string contentType, string contentDisposition, FileStream fileStream)
        {
            ContentType = contentType;
            ContentDisposition = contentDisposition;
            FileStream = fileStream;
        }

        public string ContentType { get; }
        public string ContentDisposition { get; }
        public FileStream FileStream { get; }
    }
}
