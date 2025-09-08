using ArchivistClient;
using ArchivistClient.Hooks;
using OverwatchTranscript;
using Utils;

namespace ArchivistPlugin.OverwatchSupport
{
    public class ArchivistNodeTranscriptWriter : IArchivistNodeHooks
    {
        private readonly ITranscriptWriter writer;
        private readonly IdentityMap identityMap;
        private readonly string name;
        private int identityIndex = -1;
        private readonly List<(DateTime, OverwatchArchivistEvent)> pendingEvents = new List<(DateTime, OverwatchArchivistEvent)>();

        public ArchivistNodeTranscriptWriter(ITranscriptWriter writer, IdentityMap identityMap, string name)
        {
            this.writer = writer;
            this.identityMap = identityMap;
            this.name = name;
        }

        public void OnNodeStarting(DateTime startUtc, string image, EthAccount? ethAccount)
        {
            WriteArchivistEvent(startUtc, e =>
            {
                e.NodeStarting = new NodeStartingEvent
                {
                    Image = image,
                    EthAddress = ethAccount != null ? ethAccount.ToString() : ""
                };
            });
        }

        public void OnNodeStarted(IArchivistNode node, string peerId, string nodeId)
        {
            if (string.IsNullOrEmpty(peerId) || string.IsNullOrEmpty(nodeId))
            {
                throw new Exception("Node started - peerId and/or nodeId unknown.");
            }

            identityMap.Add(name, peerId, nodeId);
            identityIndex = identityMap.GetIndex(name);

            WriteArchivistEvent(e =>
            {
                e.NodeStarted = new NodeStartedEvent
                {
                };
            });
        }

        public void OnNodeStopping()
        {
            WriteArchivistEvent(e =>
            {
                e.NodeStopping = new NodeStoppingEvent
                {
                };
            });
        }

        public void OnFileDownloading(ContentId cid)
        {
            WriteArchivistEvent(e =>
            {
                e.FileDownloading = new FileDownloadingEvent
                {
                    Cid = cid.Id
                };
            });
        }

        public void OnFileDownloaded(ByteSize size, ContentId cid)
        {
            WriteArchivistEvent(e =>
            {
                e.FileDownloaded = new FileDownloadedEvent
                {
                    Cid = cid.Id,
                    ByteSize = size.SizeInBytes
                };
            });
        }

        public void OnFileUploading(string uid, ByteSize size)
        {
            WriteArchivistEvent(e =>
            {
                e.FileUploading = new FileUploadingEvent
                {
                    UniqueId = uid,
                    ByteSize = size.SizeInBytes
                };
            });
        }

        public void OnFileUploaded(string uid, ByteSize size, ContentId cid)
        {
            WriteArchivistEvent(e =>
            {
                e.FileUploaded = new FileUploadedEvent
                { 
                    UniqueId = uid,
                    Cid = cid.Id,
                    ByteSize = size.SizeInBytes
                };
            });
        }

        public void OnStorageContractSubmitted(StoragePurchaseContract storagePurchaseContract)
        {
            WriteArchivistEvent(e =>
            {
                e.StorageContractSubmitted = new StorageContractSubmittedEvent
                {
                    PurchaseId = storagePurchaseContract.PurchaseId,
                    PurchaseRequest = storagePurchaseContract.Purchase
                };
            });
        }

        public void OnStorageContractUpdated(StoragePurchase purchaseStatus)
        {
            WriteArchivistEvent(e =>
            {
                e.StorageContractUpdated = new StorageContractUpdatedEvent
                {
                    StoragePurchase = purchaseStatus
                };
            });
        }

        public void OnStorageAvailabilityCreated(StorageAvailability response)
        {
            WriteArchivistEvent(e =>
            {
                e.StorageAvailabilityCreated = new StorageAvailabilityCreatedEvent
                {
                    StorageAvailability = response
                };
            });
        }

        private void WriteArchivistEvent(Action<OverwatchArchivistEvent> action)
        {
            WriteArchivistEvent(DateTime.UtcNow, action);
        }

        private void WriteArchivistEvent(DateTime utc, Action<OverwatchArchivistEvent> action)
        {
            var e = new OverwatchArchivistEvent
            {
                NodeIdentity = identityIndex
            };

            action(e);

            if (identityIndex < 0)
            {
                // If we don't know our id, don't write the events yet.
                AddToCache(utc, e);
            }
            else
            {
                e.Write(utc, writer);

                // Write any events that we cached when we didn't have our id yet.
                WriteAndClearCache();
            }
        }

        private void AddToCache(DateTime utc, OverwatchArchivistEvent e)
        {
            pendingEvents.Add((utc, e));
        }

        private void WriteAndClearCache()
        {
            if (pendingEvents.Any())
            {
                foreach (var pair in pendingEvents)
                {
                    pair.Item2.NodeIdentity = identityIndex;
                    pair.Item2.Write(pair.Item1, writer);
                }
                pendingEvents.Clear();
            }
        }
    }
}
