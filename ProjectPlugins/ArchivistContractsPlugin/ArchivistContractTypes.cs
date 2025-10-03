using ArchivistContractsPlugin.Marketplace;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using System.Reflection;

namespace ArchivistContractsPlugin
{
    public class ArchivistContractTypes
    {
        private static MarketplaceFunctionTypes? functionTypes = null;

        public static MarketplaceFunctionTypes GetMarketplaceFunctionTypes()
        {
            if (functionTypes != null) return functionTypes;

            var marketplaceType = typeof(MarketplaceDeployment);
            var contractNamespace = marketplaceType.Namespace;

            var allTypes = Assembly.GetAssembly(marketplaceType)!.GetTypes()
                .Where(t =>
                    t.IsPublic &&
                    t.Namespace == contractNamespace &&
                    t.BaseType != typeof(FunctionMessage) &&
                    typeof(FunctionMessage).IsAssignableFrom(t)).ToArray();

            var views = new List<Type>();
            var transactions = new List<Type>();
            var other = new List<Type>();

            foreach (var t in allTypes)
            {
                var isView = typeof(IViewFunction).IsAssignableFrom(t);
                var isTransaction = typeof(ITransactionFunction).IsAssignableFrom(t);
                if (isView && isTransaction) throw new Exception($"Type {t.Name} is both {nameof(IViewFunction)} and {nameof(ITransactionFunction)}. Should be one or other, not both.");

                if (isView) views.Add(t);
                else if (isTransaction) transactions.Add(t);
                else other.Add(t);
            }

            if (other.Count > 0)
            {
                throw new Exception($"Missing {nameof(IViewFunction)} and {nameof(ITransactionFunction)} interfaces for " +
                    $"function types: {string.Join(", ", other.Select(o => o.Name).ToArray())}.");
            }

            functionTypes = new MarketplaceFunctionTypes(views.ToArray(), transactions.ToArray());
            return functionTypes;
        }

        private static Type[]? eventTypes = null;
        public static Type[] GetMarketplaceEventTypes()
        {
            if (eventTypes != null) return eventTypes;

            var marketplaceType = typeof(MarketplaceDeployment);
            var contractNamespace = marketplaceType.Namespace;

            eventTypes = Assembly.GetAssembly(marketplaceType)!.GetTypes()
                .Where(t =>
                    t.IsPublic &&
                    t.Namespace == contractNamespace &&
                    t.BaseType != typeof(IEventDTO) &&
                    typeof(IEventDTO).IsAssignableFrom(t)).ToArray();

            return eventTypes;
        }
    }

    public class MarketplaceFunctionTypes 
    {
        public MarketplaceFunctionTypes(Type[] viewFunctions, Type[] transactionFunctions)
        {
            ViewFunctions = viewFunctions;
            TransactionFunctions = transactionFunctions;
        }

        public Type[] ViewFunctions { get; }
        public Type[] TransactionFunctions { get; }
    }
}
