using ArchivistContractsPlugin;
using ArchivistContractsPlugin.Marketplace;
using ChainStateAPI.Database;
using Nethereum.Hex.HexConvertors.Extensions;

namespace ChainStateAPI.Services
{
    public interface IMapperService
    {
        string Map(byte[] requestId);
        byte[] Map(string requestId);
        StorageRequested Map(StorageRequestedEventDTO eventDTO);
        ContractStarted Map(RequestFulfilledEventDTO eventDTO);
        ContractFailed Map(RequestFailedEventDTO eventDTO);
        SlotFilled Map(SlotFilledEventDTO eventDTO);
        SlotFreed Map(SlotFreedEventDTO eventDTO);
        SlotReservationsFull Map(SlotReservationsFullEventDTO eventDTO);
        StorageContract Map(CacheRequest chainRequest);
    }

    public class MapperService : IMapperService
    {
        public string Map(byte[] requestId)
        {
            return requestId.ToHex().ToLowerInvariant();
        }

        public byte[] Map(string requestId)
        {
            return requestId.HexToByteArray();
        }
    }
}
