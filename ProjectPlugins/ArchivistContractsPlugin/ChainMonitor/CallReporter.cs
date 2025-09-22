using ArchivistContractsPlugin.Marketplace;
using Nethereum.Contracts;
using Nethereum.RPC.Eth.DTOs;
using Newtonsoft.Json;

namespace ArchivistContractsPlugin.ChainMonitor
{
    public class CallReporter
    {
        private readonly List<FunctionCallReport> reports;
        private readonly Transaction t;
        private readonly DateTime blockUtc;
        private readonly ulong blockNumber;

        public CallReporter(List<FunctionCallReport> reports, Transaction t, DateTime blockUtc, ulong blockNumber)
        {
            this.reports = reports;
            this.t = t;
            this.blockUtc = blockUtc;
            this.blockNumber = blockNumber;
        }

        public void Run(Action<MarkProofAsMissingFunction> onMarkedAsMissing)
        {
            // We want to report all calls, but we have a special interest in
            // MarkProofAsMissingFunction calls: The period report should reflect
            // this call, so it can accurately report missed proofs.
            CreateFunctionCallReport(onMarkedAsMissing);

            // These are view function.
            // They should not end up in transactions on the blockchain.
            // If they do, we'll throw here.
            ThrowForCall<CanMarkProofAsMissingFunction>();
            ThrowForCall<CanReserveSlotFunction>();
            ThrowForCall<ConfigurationFunction>();
            ThrowForCall<TokenFunction>();
            ThrowForCall<CurrentCollateralFunction>();
            ThrowForCall<GetRequestFunction>();
            ThrowForCall<GetHostFunction>();
            ThrowForCall<MyRequestsFunction>();
            ThrowForCall<MySlotsFunction>();
            ThrowForCall<RequestStateFunction>();
            ThrowForCall<SlotStateFunction>();
            ThrowForCall<RequestEndFunction>();
            ThrowForCall<RequestExpiryFunction>();
            ThrowForCall<MissingProofsFunction>();
            ThrowForCall<IsProofRequiredFunction>();
            ThrowForCall<WillProofBeRequiredFunction>();
            ThrowForCall<GetPointerFunction>();
            ThrowForCall<GetActiveSlotFunction>();
            ThrowForCall<GetChallengeFunction>();
            ThrowForCall<SlotProbabilityFunction>();


            // These functions are expected.
            // We create reports for them.
            CreateFunctionCallReport<FillSlotFunction>();
            CreateFunctionCallReport<FreeSlot1Function>();
            CreateFunctionCallReport<FreeSlotFunction>();
            CreateFunctionCallReport<RequestStorageFunction>();
            CreateFunctionCallReport<ReserveSlotFunction>();
            CreateFunctionCallReport<SubmitProofFunction>();
            CreateFunctionCallReport<WithdrawFundsFunction>();
            CreateFunctionCallReport<WithdrawFunds1Function>();
        }

        private void CreateFunctionCallReport<TFunc>() where TFunc : FunctionMessage, new()
        {
            CreateFunctionCallReport<TFunc>(f => { });
        }

        private void CreateFunctionCallReport<TFunc>(Action<TFunc> onCall) where TFunc : FunctionMessage, new()
        {
            if (t.IsTransactionForFunctionMessage<TFunc>())
            {
                var func = t.DecodeTransactionToFunctionMessage<TFunc>();
                reports.Add(new FunctionCallReport(blockUtc, blockNumber, typeof(TFunc).Name, JsonConvert.SerializeObject(func)));
                onCall(func);
            }
        }

        private void ThrowForCall<TFunc>() where TFunc : FunctionMessage, new()
        {
            if (t.IsTransactionForFunctionMessage<TFunc>())
            {
                throw new Exception($"Call to '{typeof(TFunc).Name}' found in on-chain transaction. This should not happen.");
            }
        }
    }
}
