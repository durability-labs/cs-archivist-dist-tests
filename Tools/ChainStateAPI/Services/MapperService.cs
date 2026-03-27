using ArchivistContractsPlugin;
using ArchivistContractsPlugin.Marketplace;
using ChainStateAPI.Controllers;
using ChainStateAPI.Database;
using Nethereum.Hex.HexConvertors.Extensions;
using Utils;

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
        StorageContract Map(string requestId, CacheRequest chainRequest);
        ContractChainValues Map(StorageContract contract);
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

        public StorageRequested Map(StorageRequestedEventDTO eventDTO)
        {
            var result = MapContractEvent<StorageRequested>(eventDTO);
            result.Ask = Map(eventDTO.Expiry, eventDTO.Ask);
            return result;
        }

        public ContractStarted Map(RequestFulfilledEventDTO eventDTO)
        {
            return MapContractEvent<ContractStarted>(eventDTO);
        }

        public ContractFailed Map(RequestFailedEventDTO eventDTO)
        {
            return MapContractEvent<ContractFailed>(eventDTO);
        }

        public SlotFilled Map(SlotFilledEventDTO eventDTO)
        {
            var result = MapSlotEvent<SlotFilled>(eventDTO);
            result.HostAddress = eventDTO.Host.Address;
            return result;
        }

        public SlotFreed Map(SlotFreedEventDTO eventDTO)
        {
            return MapSlotEvent<SlotFreed>(eventDTO);
        }

        public SlotReservationsFull Map(SlotReservationsFullEventDTO eventDTO)
        {
            return MapSlotEvent<SlotReservationsFull>(eventDTO);
        }

        public StorageContract Map(string requestId, CacheRequest chainRequest)
        {
            return new StorageContract
            {
                RequestId = requestId,
                CreationUtc = chainRequest.ExpiryUtc - TimeSpan.FromSeconds(chainRequest.Request.Expiry),
                ExpiryUtc = chainRequest.ExpiryUtc,
                FinishedUtc = chainRequest.FinishUtc,
                ClientAddress = chainRequest.Request.Client,
                Cid = chainRequest.Request.Content.Cid,
                CidStr = "z" + Base58.Encode(chainRequest.Request.Content.Cid),
                MerkleRoot = chainRequest.Request.Content.MerkleRoot,
                Ask = Map(chainRequest.Request.Expiry, chainRequest.Request.Ask)
            };
        }

        private StorageContractAsk Map(ulong expiry, Ask ask)
        {
            return new StorageContractAsk
            {
                Expiry = expiry,
                CollateralPerByte = ask.CollateralPerByte,
                Duration = ask.Duration,
                MaxSlotLoss = ask.MaxSlotLoss,
                PricePerBytePerSecond = ask.PricePerBytePerSecond,
                ProofProbability = ask.ProofProbability,
                Slots = ask.Slots,
                SlotSize = ask.SlotSize,
            };
        }

        public ContractChainValues Map(StorageContract contract)
        {
            return new ContractChainValues
            {
                Client = contract.ClientAddress,
                Content = new ContractContent
                {
                    Cid = contract.Cid,
                    MerkleRoot = contract.MerkleRoot,
                },
                Ask = contract.Ask,
            };
        }

        private T MapContractEvent<T>(IHasBlockAndRequestId eventDTO) where T : ContractEvent, new()
        {
            return new T
            {
                RequestId = Map(eventDTO.RequestId),
                BlockNumber = eventDTO.Block.BlockNumber,
                Utc = eventDTO.Block.Utc,
            };
        }

        private T MapSlotEvent<T>(IHasBlockRequestIdSlotIndex eventDTO) where T : SlotEvent, new()
        {
            var result = MapContractEvent<T>(eventDTO);
            result.SlotIndex = eventDTO.SlotIndex;
            return result;
        }
    }
}
