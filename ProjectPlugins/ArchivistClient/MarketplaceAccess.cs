using ArchivistClient.Hooks;
using Logging;
using Utils;

namespace ArchivistClient
{
    public interface IMarketplaceAccess
    {
        void MakeStorageAvailable(CreateStorageAvailability availability);
        StorageAvailability GetAvailability();
        StorageSlot[] GetSlots();
        IStoragePurchaseContract RequestStorage(StoragePurchaseRequest purchase);
    }

    public class MarketplaceAccess : IMarketplaceAccess
    {
        private readonly ILog log;
        private readonly ArchivistAccess archivistAccess;
        private readonly IArchivistNodeHooks hooks;

        public MarketplaceAccess(ILog log, ArchivistAccess archivistAccess, IArchivistNodeHooks hooks)
        {
            this.log = log;
            this.archivistAccess = archivistAccess;
            this.hooks = hooks;
        }

        public IStoragePurchaseContract RequestStorage(StoragePurchaseRequest purchase)
        {
            purchase.Log(log);
            if (purchase.Expiry < TimeSpan.FromMinutes(6.0)) throw new Exception($"Expiry should be at least 6 minutes. Was: {Time.FormatDuration(purchase.Expiry)}");
            if (purchase.Duration < purchase.Expiry) throw new Exception($"Duration must be larger than expiry. Duration: {Time.FormatDuration(purchase.Duration)} Expiry: {Time.FormatDuration(purchase.Expiry)}");

            var swResult = Stopwatch.Measure(log, nameof(RequestStorage), () =>
            {
                return archivistAccess.RequestStorage(purchase);
            });

            var response = swResult.Value;

            if (string.IsNullOrEmpty(response) ||
                response == "Unable to encode manifest" ||
                response == "Purchasing not available" ||
                response == "Expiry required" ||
                response == "Expiry needs to be in future" ||
                response == "Expiry has to be before the request's end (now + duration)")
            {
                throw new InvalidOperationException(response);
            }

            Log($"Storage requested successfully. PurchaseId: '{response}'.");

            var logName = $"<Purchase-{response.Substring(0, 3)}>";
            log.AddStringReplace(response, logName);
            return new StoragePurchaseContract(log, archivistAccess, response, purchase, hooks);
        }

        public void MakeStorageAvailable(CreateStorageAvailability availability)
        {
            availability.Log(log);

            archivistAccess.SalesAvailability(availability);

            Log($"Storage successfully made available.");
            hooks.OnStorageAvailabilityCreated();
        }

        public StorageAvailability GetAvailability()
        {
            var result = archivistAccess.GetAvailability();
            Log($"Got availability:");
            result.Log(log);
            return result;
        }

        public StorageSlot[] GetSlots()
        {
            var result = archivistAccess.GetSlots();
            Log("Active slots: " + result.Length);
            foreach (var s in result) s.Log(log);
            return result;
        }

        private void Log(string msg)
        {
            log.Log(msg);
        }
    }

    public class MarketplaceUnavailable : IMarketplaceAccess
    {
        public void MakeStorageAvailable(CreateStorageAvailability availability)
        {
            Unavailable();
            throw new NotImplementedException();
        }

        public IStoragePurchaseContract RequestStorage(StoragePurchaseRequest purchase)
        {
            Unavailable();
            throw new NotImplementedException();
        }

        public StorageAvailability GetAvailability()
        {
            Unavailable();
            throw new NotImplementedException();
        }

        public StorageSlot[] GetSlots()
        {
            Unavailable();
            throw new NotImplementedException();
        }

        private void Unavailable()
        {
            FrameworkAssert.Fail("Incorrect test setup: Marketplace was not enabled for this group of Archivist nodes. Add 'EnableMarketplace(...)' after 'SetupArchivistNodes()' to enable it.");
            throw new InvalidOperationException();
        }
    }
}
