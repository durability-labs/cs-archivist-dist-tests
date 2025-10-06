using BlockchainUtils;
using Logging;
using Nethereum.Web3;
using Utils;

namespace NethereumWorkflow
{
    public class NethereumInteractionCreator
    {
        private readonly ILog log;
        private readonly BlockCache blockCache;
        private readonly string rpcUrl;
        private readonly string privateKey;

        public NethereumInteractionCreator(ILog log, BlockCache blockCache, string rpcUrl, string privateKey)
        {
            this.log = log;
            this.blockCache = blockCache;
            this.rpcUrl = rpcUrl;
            this.privateKey = privateKey;
            
            log.Debug($"Setup {nameof(NethereumInteractionCreator)} to " + rpcUrl);
        }

        public NethereumInteraction CreateWorkflow()
        {
            return new NethereumInteraction(log, CreateWeb3(), blockCache);
        }

        public EthAddress GetEthAddress()
        {
            var account = new Nethereum.Web3.Accounts.Account(privateKey);
            return new EthAddress(account.Address);
        }

        private Web3 CreateWeb3()
        {
            var account = new Nethereum.Web3.Accounts.Account(privateKey);
            return new Web3(account, rpcUrl);
        }
    }
}
