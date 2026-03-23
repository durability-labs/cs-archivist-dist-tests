using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Converters;
using System.Numerics;
using System.Text.Json.Serialization;

namespace ChainStateAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MomentRequestsController : ControllerBase
    {
        [HttpPost]
        public ActiveContractsResponse GetActiveContracts([FromBody] MomentRequest request)
        {
            return new ActiveContractsResponse();
        }

        [HttpPost]
        public ContractsStateResponse GetContractsState([FromBody] ContractsStateRequest request)
        {
            return new ContractsStateResponse();
        }
    }

    public class MomentRequest
    {
        public DateTime Utc { get; set; }
    }

    public class ContractsStateRequest : MomentRequest
    {
        public string[] ContractIds { get; set; } = Array.Empty<string>();
    }

    public class ActiveContractsResponse
    {
        public string[] ContractIds { get; set; } = Array.Empty<string>();
    }

    public class ContractsStateResponse
    {
        public DateTime Utc { get; set; }
        public ContractState[] ContractStates { get; set; } = Array.Empty<ContractState>();
    }

    public class ContractState
    {
        public string ContractId { get; set; } = string.Empty;
        public ContractChainValues ContractChainValues { get; set; } = new();
        public ContractDerivedValues ContractDerivedValues { get; set; } = new();
    }

    public class ContractDerivedValues
    {
        public RequestState State { get; set; } = RequestState.Unknown;
        public DateTime CreationUtc { get; set; }
        public DateTime ExpiryUtc { get; set; }
        public DateTime FinishedUtc { get; set; }
        public ContractSlot[] Slots { get; set; } = Array.Empty<ContractSlot>();
        public string[] ExtendsPreviousContracts { get; set; } = Array.Empty<string>();
    }

    public class ContractSlot
    {
        public ulong SlotIndex { get; set; }
        public string HostOrEmpty { get; set; } = string.Empty;
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum RequestState
    {
        Unknown,
        New,
        Started,
        Cancelled,
        Finished,
        Failed
    }

    public class ContractChainValues
    {
        public string Client { get; set; } = string.Empty;
        public ContractAsk Ask { get; set; } = new();
        public ContractContent Content { get; set; } = new();
        public ulong Expiry { get; set; }
    }

    public class ContractAsk
    {
        public BigInteger ProofProbability { get; set; }
        public BigInteger PricePerBytePerSecond { get; set; }
        public BigInteger CollateralPerByte { get; set; }
        public ulong Slots { get; set; }
        public ulong SlotSize { get; set; }
        public ulong Duration { get; set; }
        public ulong MaxSlotLoss { get; set; }
    }

    public class ContractContent
    {
        public byte[] Cid { get; set; } = Array.Empty<byte>();
        public byte[] MerkleRoot { get; set; } = Array.Empty<byte>();
    }
}
