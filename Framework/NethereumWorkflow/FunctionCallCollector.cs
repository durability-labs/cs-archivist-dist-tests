using BlockchainUtils;
using Nethereum.Contracts;
using Nethereum.RPC.Eth.DTOs;

namespace NethereumWorkflow
{
    public interface IFunctionCallCollector
    {
        string Name { get; }
        bool IsFunction(Transaction transaction);
        void AddCall(BlockTimeEntry block, Transaction transaction);
        IFunctionCall[] GetCalls();
    }

    public interface IFunctionCall
    {
        BlockTimeEntry Block { get; }
        object GetCall();
    }

    public class FunctionCallCollector<TFunc> : IFunctionCallCollector where TFunc : FunctionMessage, new()
    {
        public FunctionCallCollector()
        {
        }

        public bool IsFunction(Transaction transaction)
        {
            return transaction.IsTransactionForFunctionMessage<TFunc>();
        }

        public void AddCall(BlockTimeEntry block, Transaction transaction)
        {
            var func = transaction.DecodeTransactionToFunctionMessage<TFunc>();
            Calls.Add(new FunctionCall<TFunc>(block, func));
        }

        public string Name => typeof(TFunc).Name;
        public List<FunctionCall<TFunc>> Calls { get; } = new();

        public IFunctionCall[] GetCalls()
        {
            return Calls.ToArray();
        }
    }

    public class FunctionCall<TFunc> : IFunctionCall where TFunc : FunctionMessage, new()
    {
        public FunctionCall(BlockTimeEntry block, TFunc call)
        {
            Block = block;
            Call = call;

            if (Call is IHasBlock hasBlock)
            {
                hasBlock.Block = block;
            }
        }

        public BlockTimeEntry Block { get; }
        public TFunc Call { get; }

        public object GetCall()
        {
            return Call;
        }
    }
}
