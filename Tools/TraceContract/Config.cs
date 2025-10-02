namespace TraceContract
{
    public class Config
    {
        public string DataDir { get; } = "tracecontract_datadir";
        public string RpcEndpoint { get; } = "https://rpc-arbitrum-discordbot.testnet.archivist.storage";
        public int GethPort { get; } = 443;
        public string MarketplaceAddress { get; } = "0x9fec9f5C5D6232E3cE55B92FC04758a41A528d2b";
        public string TokenAddress { get; } = "0x3b7412Ee1144b9801341A4F391490eB735DDc005";
        public string Abi { get; } = "[{\"inputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"constructor\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"spender\",\"type\":\"address\"},{\"internalType\":\"uint256\",\"name\":\"allowance\",\"type\":\"uint256\"},{\"internalType\":\"uint256\",\"name\":\"needed\",\"type\":\"uint256\"}],\"name\":\"ERC20InsufficientAllowance\",\"type\":\"error\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"sender\",\"type\":\"address\"},{\"internalType\":\"uint256\",\"name\":\"balance\",\"type\":\"uint256\"},{\"internalType\":\"uint256\",\"name\":\"needed\",\"type\":\"uint256\"}],\"name\":\"ERC20InsufficientBalance\",\"type\":\"error\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"approver\",\"type\":\"address\"}],\"name\":\"ERC20InvalidApprover\",\"type\":\"error\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"receiver\",\"type\":\"address\"}],\"name\":\"ERC20InvalidReceiver\",\"type\":\"error\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"sender\",\"type\":\"address\"}],\"name\":\"ERC20InvalidSender\",\"type\":\"error\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"spender\",\"type\":\"address\"}],\"name\":\"ERC20InvalidSpender\",\"type\":\"error\"},{\"anonymous\":false,\"inputs\":[{\"indexed\":true,\"internalType\":\"address\",\"name\":\"owner\",\"type\":\"address\"},{\"indexed\":true,\"internalType\":\"address\",\"name\":\"spender\",\"type\":\"address\"},{\"indexed\":false,\"internalType\":\"uint256\",\"name\":\"value\",\"type\":\"uint256\"}],\"name\":\"Approval\",\"type\":\"event\"},{\"anonymous\":false,\"inputs\":[{\"indexed\":true,\"internalType\":\"address\",\"name\":\"from\",\"type\":\"address\"},{\"indexed\":true,\"internalType\":\"address\",\"name\":\"to\",\"type\":\"address\"},{\"indexed\":false,\"internalType\":\"uint256\",\"name\":\"value\",\"type\":\"uint256\"}],\"name\":\"Transfer\",\"type\":\"event\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"owner\",\"type\":\"address\"},{\"internalType\":\"address\",\"name\":\"spender\",\"type\":\"address\"}],\"name\":\"allowance\",\"outputs\":[{\"internalType\":\"uint256\",\"name\":\"\",\"type\":\"uint256\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"spender\",\"type\":\"address\"},{\"internalType\":\"uint256\",\"name\":\"value\",\"type\":\"uint256\"}],\"name\":\"approve\",\"outputs\":[{\"internalType\":\"bool\",\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"account\",\"type\":\"address\"}],\"name\":\"balanceOf\",\"outputs\":[{\"internalType\":\"uint256\",\"name\":\"\",\"type\":\"uint256\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"decimals\",\"outputs\":[{\"internalType\":\"uint8\",\"name\":\"\",\"type\":\"uint8\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"holder\",\"type\":\"address\"},{\"internalType\":\"uint256\",\"name\":\"amount\",\"type\":\"uint256\"}],\"name\":\"mint\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"name\",\"outputs\":[{\"internalType\":\"string\",\"name\":\"\",\"type\":\"string\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"symbol\",\"outputs\":[{\"internalType\":\"string\",\"name\":\"\",\"type\":\"string\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"totalSupply\",\"outputs\":[{\"internalType\":\"uint256\",\"name\":\"\",\"type\":\"uint256\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"to\",\"type\":\"address\"},{\"internalType\":\"uint256\",\"name\":\"value\",\"type\":\"uint256\"}],\"name\":\"transfer\",\"outputs\":[{\"internalType\":\"bool\",\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"from\",\"type\":\"address\"},{\"internalType\":\"address\",\"name\":\"to\",\"type\":\"address\"},{\"internalType\":\"uint256\",\"name\":\"value\",\"type\":\"uint256\"}],\"name\":\"transferFrom\",\"outputs\":[{\"internalType\":\"bool\",\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"nonpayable\",\"type\":\"function\"}]";

        /// <summary>
        /// Naming things is hard.
        /// If the storage request is created at T=0, then fetching of the
        /// storage node logs will begin from T=0 minus 'LogStartBeforeStorageContractStarts'.
        /// </summary>
        public TimeSpan LogStartBeforeStorageContractStarts { get; } = TimeSpan.FromMinutes(1.0);

        public string ElasticSearchUrl
        {
            get
            {
                return GetEnvVar("ES_HOST", "es_host");
            }
        }

        public string[] StorageNodesKubernetesPodNames = [
            "archivist-1-1",
            "archivist-2-1",
            "archivist-3-1",
            "archivist-4-1",
            "archivist-5-1",
            "archivist-6-1",
            "archivist-7-1",
            "archivist-8-1",
            "archivist-9-1",
            "archivist-10-1",
            // "archivist-validator-1-1",
        ];

        public Dictionary<string, string> LogReplacements = new()
        {
            { "0x3620ec38d88e9f0cf7feceebf97864f27676aa3e", "archivist-01" },
            { "0xd80dc50af2a826f2cddc13840d05aed4ee6536c3", "archivist-02" },
            { "0x2d1cd0fa0c7e0d29e7b2482b9ff87d5e7b76b905", "archivist-03" },
            { "0xd47063bb6e56c9a6edb7612d33ad7d49eeb55ee0", "archivist-04" },
            { "0x069da63e29b12a3828984379fcbd7dd3ee3774aa", "archivist-05" },
            { "0x43fcceb2a9ce4761ccaa4c9f8d390c7581c190aa", "archivist-06" },
            { "0x1a30cef06dbbf8ec25062e4e8d22e8df292f5054", "archivist-07" },
            { "0xe169b5dcbae9a7392072323aaf5a677a33d67ecd", "archivist-08" },
            { "0x21f7428619ef9f53addc5dab6723c822a8a96b42", "archivist-09" },
            { "0xf9bd20512de2d5ca0dcfd8d3cd08a2821917797a", "archivist-10" }
        };

        public string GetElasticSearchUsername()
        {
            return GetEnvVar("ES_USERNAME", "username");
        }

        public string GetElasticSearchPassword()
        {
            return GetEnvVar("ES_PASSWORD", "password");
        }

        public string GetOuputFolder()
        {
            return GetEnvVar("OUTPUT_FOLDER", "/output");
        }

        private string GetEnvVar(string name, string defaultValue)
        {
            var v = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrEmpty(v)) return defaultValue;
            return v;
        }
    }
}
