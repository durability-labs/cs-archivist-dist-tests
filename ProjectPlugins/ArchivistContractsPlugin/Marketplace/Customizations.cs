#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using BlockchainUtils;
using Nethereum.Hex.HexConvertors.Extensions;
using Newtonsoft.Json;
using ArchivistClient;
using Utils;

namespace ArchivistContractsPlugin.Marketplace
{
    public interface IViewFunction
    {
        // Marker interface for all view-function types.
    }

    public interface ITransactionFunction
    {
        // Marker for all other functions.
    }

    public interface IHasRequestId
    {
        byte[] RequestId { get; set; }
    }

    public interface IHasBlockAndRequestId : IHasBlock, IHasRequestId
    {
    }

    public interface IHasSlotIndex
    {
        ulong SlotIndex { get; set; }
    }

    public partial class Request
    {
        [JsonIgnore]
        public EthAddress ClientAddress { get { return new EthAddress(Client); } }
    }

    public partial class StorageRequestedEventDTO : IHasBlockAndRequestId
    {
        [JsonIgnore]
        public BlockTimeEntry Block { get; set; }
    }

    public partial class RequestFulfilledEventDTO : IHasBlockAndRequestId
    {
        [JsonIgnore]
        public BlockTimeEntry Block { get; set; }
    }

    public partial class RequestCancelledEventDTO : IHasBlockAndRequestId
    {
        [JsonIgnore]
        public BlockTimeEntry Block { get; set; }
    }

    public partial class RequestFailedEventDTO : IHasBlockAndRequestId
    {
        [JsonIgnore]
        public BlockTimeEntry Block { get; set; }
    }

    public partial class SlotFilledEventDTO : IHasBlockAndRequestId, IHasSlotIndex
    {
        [JsonIgnore]
        public BlockTimeEntry Block { get; set; }
        public EthAddress Host { get; set; }

        public override string ToString()
        {
            return $"SlotFilled:[host:{Host} request:{RequestId.ToHex()} slotIndex:{SlotIndex}]";
        }
    }

    public partial class SlotFreedEventDTO : IHasBlockAndRequestId, IHasSlotIndex
    {
        [JsonIgnore]
        public BlockTimeEntry Block { get; set; }
    }

    public partial class SlotReservationsFullEventDTO : IHasBlockAndRequestId, IHasSlotIndex
    {
        [JsonIgnore]
        public BlockTimeEntry Block { get; set; }
    }

    public partial class ProofSubmittedEventDTO : IHasBlock
    {
        [JsonIgnore]
        public BlockTimeEntry Block { get; set; }
    }

    public partial class ReserveSlotFunction : IHasBlockAndRequestId, IHasSlotIndex
    {
        [JsonIgnore]
        public BlockTimeEntry Block { get; set; }
    }

    public partial class MarketplaceConfig : IMarketplaceConfigInput
    {
        public int MaxNumberOfSlashes
        {
            get
            {
                if (Collateral == null) return -1;
                return Collateral.MaxNumberOfSlashes;
            }
        }

        public TimeSpan PeriodDuration
        {
            get
            {
                if (Proofs == null) return TimeSpan.MinValue;
                return TimeSpan.FromSeconds(this.Proofs.Period);
            }
        }
    }

    public partial class CanMarkProofAsMissingFunction : IViewFunction { }
    public partial class CanReserveSlotFunction : IViewFunction { }
    public partial class ConfigurationFunction : IViewFunction { }
    public partial class TokenFunction : IViewFunction { }
    public partial class CurrentCollateralFunction : IViewFunction { }
    public partial class GetRequestFunction : IViewFunction { }
    public partial class GetHostFunction : IViewFunction { }
    public partial class MyRequestsFunction : IViewFunction { }
    public partial class MySlotsFunction : IViewFunction { }
    public partial class RequestStateFunction : IViewFunction { }
    public partial class SlotStateFunction : IViewFunction { }
    public partial class RequestEndFunction : IViewFunction { }
    public partial class RequestExpiryFunction : IViewFunction { }
    public partial class MissingProofsFunction : IViewFunction { }
    public partial class IsProofRequiredFunction : IViewFunction { }
    public partial class WillProofBeRequiredFunction : IViewFunction { }
    public partial class GetPointerFunction : IViewFunction { }
    public partial class GetActiveSlotFunction : IViewFunction { }
    public partial class GetChallengeFunction : IViewFunction { }
    public partial class SlotProbabilityFunction : IViewFunction { }

    public partial class FillSlotFunction : ITransactionFunction { }
    public partial class FreeSlot1Function : ITransactionFunction { }
    public partial class FreeSlotFunction : ITransactionFunction { }
    public partial class RequestStorageFunction : ITransactionFunction { }
    public partial class ReserveSlotFunction : ITransactionFunction { }
    public partial class SubmitProofFunction : ITransactionFunction { }
    public partial class WithdrawFundsFunction : ITransactionFunction { }
    public partial class WithdrawFunds1Function : ITransactionFunction { }
    public partial class MarkProofAsMissingFunction : ITransactionFunction { }
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
