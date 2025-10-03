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
            "archivist-storage-1-1",
            "archivist-storage-2-1",
            "archivist-storage-3-1",
            "archivist-storage-4-1",
            "archivist-storage-5-1",
            "archivist-storage-6-1",
            "archivist-storage-7-1",
            "archivist-storage-8-1",
            "archivist-storage-9-1",
            "archivist-storage-10-1",
            // "archivist-validator-1-1",
        ];

        public Dictionary<string, string> LogReplacements = new()
        {
            { "0xc4566f67a64A81Fee99Ef868B456f909db93Ef85", "archivist-storage-01" },
            { "0xF18EEAF1ead1f209E5db07eaB176962ff9C46939", "archivist-storage-02" },
            { "0xBF455fbe817943D14bF215864889969e4A396AE7", "archivist-storage-03" },
            { "0xa377964E7E26a561c2a19b5Da361c3835993a1df", "archivist-storage-04" },
            { "0xf9374e41cf3F80316e74194c9468314A5fB2Bd65", "archivist-storage-05" },
            { "0xf242F5Fee2A878b3c027CBC4f9289F04aA025BD5", "archivist-storage-06" },
            { "0xE94896F0Bd44ACeA9d38a248F5Ac08200d9c4D2d", "archivist-storage-07" },
            { "0xf13584d50ea185AE0b1C5c7ff4398aad64dEf49a", "archivist-storage-08" },
            { "0x511a8f1938a5E0C068F75143454197CF3FCC606f", "archivist-storage-09" },
            { "0x1E36465bd79a6B1Cdf2bC1478cFADe8E3b732f12", "archivist-storage-10" },
            { "0xad6b4700A8A946bE1a5006C2424234bC91a7f4a9", "archivist-storage-11" },
            { "0xD48028b291BBADC5d422DF9d076f25a57e6C7B0f", "archivist-storage-12" },
            { "0x98271F9c33136aA9E91E9A5ac6A479A7616Cfe26", "archivist-storage-13" },
            { "0x2C36C54ABf5B4893228AF477DE25E55A97730FD2", "archivist-storage-14" },
            { "0xfA7Bb406Fa63Ff2d1884b0B369554908E3CC90ef", "archivist-storage-15" },
            { "0x3E0BD7effD3fb32384Ad3A3209dEfE44716B8A1E", "archivist-storage-16" },
            { "0xad3dFaA627ac75cdf066BE6A8D91cfae63956C55", "archivist-storage-17" },
            { "0x849b6f8af3d468F1841B17F77c94FED47Cf03AD4", "archivist-storage-18" },
            { "0xf8f8026298B1a376b76E15E398CF9F24C4908ec4", "archivist-storage-19" },
            { "0xCaAE5eAbB62B511405f70d4Cf1758cEba21609ef", "archivist-storage-20" }
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
