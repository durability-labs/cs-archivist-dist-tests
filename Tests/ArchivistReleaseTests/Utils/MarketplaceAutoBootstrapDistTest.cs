using ArchivistClient;
using ArchivistContractsPlugin;
using ArchivistContractsPlugin.ChainMonitor;
using ArchivistContractsPlugin.Marketplace;
using ArchivistPlugin;
using ArchivistTests;
using GethPlugin;
using Logging;
using Nethereum.Hex.HexConvertors.Extensions;
using NUnit.Framework;
using Utils;

namespace ArchivistReleaseTests.Utils
{
    public abstract class MarketplaceAutoBootstrapDistTest : AutoBootstrapDistTest, IPeriodMonitorEventHandler
    {
        private MarketplaceHandle handle = null!;
        protected const int StartingBalanceTST = 1000;
        protected const int StartingBalanceEth = 10;

        [SetUp]
        public void SetupMarketplace()
        {
            var geth = StartGethNode(s => s.IsMiner());
            var contracts = Ci.StartArchivistContracts(geth, BootstrapNode.Version);
            // Do not use TestRunTimeRange().From to initialize the chain monitor:
            // It'll find its earliest timestamps in the pre-mined blocks in the geth image
            // and completely screw with chain state tracking.
            // Use this moment, the start of the contract deployment instead.
            var monitor = SetupChainMonitor(GetTestLog(), geth, contracts, DateTime.UtcNow);
            handle = new MarketplaceHandle(geth, contracts, monitor);
        }

        [TearDown]
        public void TearDownMarketplace()
        {
            if (handle.ChainMonitor != null) handle.ChainMonitor.Stop();
        }

        protected IGethNode GetGeth()
        {
            return handle.Geth;
        }

        protected IArchivistContracts GetContracts()
        {
            return handle.Contracts;
        }

        protected ChainMonitor GetChainMonitor()
        {
            if (handle.ChainMonitor == null) throw new Exception($"Make sure {nameof(MonitorChainState)} is set to true.");
            return handle.ChainMonitor;
        }

        protected TimeSpan GetPeriodDuration()
        {
            var config = GetContracts().Deployment.Config;
            return TimeSpan.FromSeconds(config.Proofs.Period);
        }

        protected abstract int NumberOfHosts { get; }
        protected abstract int NumberOfClients { get; }
        protected virtual TestToken HostStartingBalance => StartingBalanceTST.Tst();
        protected virtual TimeSpan HostAvailabilityMaxDuration => TimeSpan.FromHours(3.0);
        protected virtual bool MonitorChainState { get; } = true;
        protected virtual bool MonitorProofPeriods { get; } = true;
        protected virtual bool LogPeriodReports { get; } = false;

        protected PurchaseParams DefaultPurchase { get; } = new PurchaseParams(
            nodes: DefaultStoragePurchase.MinRequiredNumberOfNodes,
            tolerance: DefaultStoragePurchase.NodeFailureTolerance,
            duration: DefaultStoragePurchase.Duration,
            uploadFilesize: DefaultStoragePurchase.UploadFileSize,
            pricePerByteSecond: DefaultStoragePurchase.PricePerBytePerSecond,
            collateralPerByte: DefaultStoragePurchase.CollateralPerByte
        );

        protected TestToken DefaultAvailabilityMaxCollateralPerByte => 999999.Tst();

        protected TimeSpan HostBlockTTL
        {
            get
            {
                // Blocks are downloaded using the default TTL when slots are being filled.
                // If the block expiries are not updated to match the storage contract
                // within 15 period durations, we assume it failed and the data should
                // be cleaned up.
                return GetPeriodDuration() * 15;
            }
        }

        protected virtual void OnPeriod(PeriodReport report)
        {
        }

        public (IArchivistNodeGroup, IArchivistNodeGroup) JumpStartHostsAndClients()
        {
            IArchivistNodeGroup hosts = null!;
            IArchivistNodeGroup clients = null!;
            var tasks = new Task[]
            {
                Task.Run(() => hosts = StartHosts()),
                Task.Run(() => clients = StartClients())
            };
            Task.WaitAll(tasks);
            return (hosts, clients);
        }

        public (IArchivistNodeGroup, IArchivistNodeGroup, IArchivistNode) JumpStart()
        {
            IArchivistNodeGroup hosts = null!;
            IArchivistNodeGroup clients = null!;
            IArchivistNode validator = null!;
            var tasks = new Task[]
            {
                Task.Run(() => hosts = StartHosts()),
                Task.Run(() => clients = StartClients()),
                Task.Run(() => validator = StartValidator())
            };
            Task.WaitAll(tasks);
            return (hosts, clients, validator);
        }

        public IArchivistNodeGroup StartHosts()
        {
            return StartHosts(s => { });
        }

        public IArchivistNodeGroup StartHosts(Action<IArchivistSetup> additional)
        {
            var hosts = StartArchivist(NumberOfHosts, s =>
            {
                s
                .WithName("host")
                .WithBlockTTL(HostBlockTTL)
                .WithBlockMaintenanceNumber(1000)
                .WithBlockMaintenanceInterval(HostBlockTTL / 2)
                .EnableMarketplace(GetGeth(), GetContracts(), m => m
                    .WithInitial(StartingBalanceEth.Eth(), HostStartingBalance)
                    .AsStorageNode()
                );
                additional(s);
            });

            var config = GetContracts().Deployment.Config;
            foreach (var host in hosts)
            {
                AssertTstBalance(host, HostStartingBalance, nameof(StartHosts));
                AssertEthBalance(host, StartingBalanceEth.Eth(), nameof(StartHosts));
                
                host.Marketplace.MakeStorageAvailable(new CreateStorageAvailability(
                    untilUtc: DateTime.UtcNow + TimeSpan.FromDays(30.0),
                    maxDuration: HostAvailabilityMaxDuration,
                    minPricePerBytePerSecond: 1.TstWei(),
                    maxCollateralPerByte: DefaultAvailabilityMaxCollateralPerByte)
                );
            }
            return hosts;
        }

        public IArchivistNode StartOneHost()
        {
            var host = StartArchivist(s => s
                .WithName("singlehost")
                .WithBlockTTL(HostBlockTTL)
                .WithBlockMaintenanceNumber(1000)
                .WithBlockMaintenanceInterval(HostBlockTTL / 2)
                .EnableMarketplace(GetGeth(), GetContracts(), m => m
                    .WithInitial(StartingBalanceEth.Eth(), HostStartingBalance)
                    .AsStorageNode()
                )
            );

            var config = GetContracts().Deployment.Config;
            AssertTstBalance(host, HostStartingBalance, nameof(StartOneHost));
            AssertEthBalance(host, StartingBalanceEth.Eth(), nameof(StartOneHost));

            host.Marketplace.MakeStorageAvailable(new CreateStorageAvailability(
                untilUtc: DateTime.UtcNow + TimeSpan.FromDays(30.0),
                maxDuration: HostAvailabilityMaxDuration,
                minPricePerBytePerSecond: 1.TstWei(),
                maxCollateralPerByte: 999999.Tst())
            );
            return host;
        }

        public void AssertHostsAreEmpty(IEnumerable<IArchivistNode> hosts)
        {
            AssertHostHasNoActiveSlots(hosts);
            AssertQuotaIsEmpty(hosts);
        }

        public void AssertHostHasNoActiveSlots(IEnumerable<IArchivistNode> hosts)
        {
            Log($"{nameof(AssertHostHasNoActiveSlots)}...");
            var retry = GetBlockTTLAssertRetry();
            retry.Run(() =>
            {
                foreach (var n in hosts)
                {
                    var slots = n.Marketplace.GetSlots();
                    if (slots.Length > 0)
                    {
                        throw new Exception($"Host {n.GetName()} has {slots.Length} slots. Expected 0.");
                    }
                }
            });
            Log($"{nameof(AssertHostHasNoActiveSlots)} OK");
        }

        public void AssertQuotaIsEmpty(IEnumerable<IArchivistNode> nodes)
        {
            Log($"{nameof(AssertQuotaIsEmpty)}...");
            var retry = GetBlockTTLAssertRetry();
            retry.Run(() =>
            {
                foreach (var n in nodes)
                {
                    var space = n.Space();
                    if (space.QuotaUsedBytes > 0)
                    {
                        throw new Exception($"Host {n.GetName()} has {space.QuotaUsedBytes} quota-bytes-used. Expected 0.");
                    }
                }
            });
            Log($"{nameof(AssertQuotaIsEmpty)} OK");
        }

        public void AssertTstBalance(IArchivistNode node, TestToken expectedBalance, string message)
        {
            AssertTstBalance(node.EthAddress, expectedBalance, message);
        }

        public void AssertTstBalance(EthAddress address, TestToken expectedBalance, string message)
        {
            var retry = GetBalanceAssertRetry();
            retry.Run(() =>
            {
                var balance = GetTstBalance(address);

                if (balance != expectedBalance)
                {
                    throw new Exception(nameof(AssertTstBalance) +
                        $" expected: {expectedBalance} but was: {balance} - message: " + message);
                }
            });
        }

        public void AssertEthBalance(IArchivistNode node, Ether expectedBalance, string message)
        {
            var retry = GetBalanceAssertRetry();
            retry.Run(() =>
            {
                var balance = GetEthBalance(node);

                if (balance != expectedBalance)
                {
                    throw new Exception(nameof(AssertEthBalance) + 
                        $" expected: {expectedBalance} but was: {balance} - message: " + message);
                }
            });
        }

        protected void AssertNoSlotsFreed(PeriodReport report)
        {
            foreach (var c in report.FunctionCalls)
            {
                Assert.That(c.Name, Is.Not.EqualTo(nameof(FreeSlot1Function)));
                Assert.That(c.Name, Is.Not.EqualTo(nameof(FreeSlotFunction)));
            }
        }

        public IArchivistNodeGroup StartClients()
        {
            return StartClients(s => { });
        }

        public IArchivistNodeGroup StartClients(Action<IArchivistSetup> additional)
        {
            return StartArchivist(NumberOfClients, s =>
            {
                s.WithName("client")
                    .EnableMarketplace(GetGeth(), GetContracts(), m => m
                    .WithInitial(StartingBalanceEth.Eth(), StartingBalanceTST.Tst()));

                additional(s);
            });
        }

        public IArchivistNode StartValidator()
        {
            return StartArchivist(s => s
                .WithName("validator")
                .EnableMarketplace(GetGeth(), GetContracts(), m => m
                    .WithInitial(StartingBalanceEth.Eth(), StartingBalanceTST.Tst())
                    .AsValidator()
                )
            );
        }

        public bool GetLogPeriodReports()
        {
            return LogPeriodReports;
        }

        public void OnPeriodReport(PeriodReport report)
        {
            OnPeriod(report);
        }

        public SlotFill[] GetOnChainSlotFills(IEnumerable<IArchivistNode> possibleHosts, string purchaseId)
        {
            var fills = GetOnChainSlotFills(possibleHosts);
            return fills.Where(f => f
                .SlotFilledEvent.RequestId.ToHex(false).ToLowerInvariant() == purchaseId.ToLowerInvariant())
                .ToArray();
        }

        public SlotFill[] GetOnChainSlotFills(IEnumerable<IArchivistNode> possibleHosts)
        {
            var events = GetContracts().GetEvents(GetTestRunTimeRange());
            var fills = events.GetEvents<SlotFilledEventDTO>();
            return fills.Select(f =>
            {
                // We can encounter a fill event that's from an old host.
                // We must disregard those.
                var host = possibleHosts.SingleOrDefault(h => h.EthAddress.Address == f.Host.Address);
                if (host == null) return null;
                return new SlotFill(f, host);
            })
            .Where(f => f != null)
            .Cast<SlotFill>()
            .ToArray();
        }

        protected void AssertClientHasPaidForContract(TestToken pricePerBytePerSecond, IArchivistNode client, IStoragePurchaseContract contract, IArchivistNodeGroup hosts)
        {
            var expectedBalance = StartingBalanceTST.Tst() - GetContractFinalCost(pricePerBytePerSecond, contract, hosts);

            AssertTstBalance(client, expectedBalance, "Client balance incorrect.");

            Log($"Client has paid for contract. Balance: {expectedBalance}");
        }

        protected void AssertHostsWerePaidForContract(TestToken pricePerBytePerSecond, IStoragePurchaseContract contract, IArchivistNodeGroup hosts)
        {
            var fills = GetOnChainSlotFills(hosts);
            var submitUtc = GetContractOnChainSubmittedUtc(contract);
            var finishUtc = submitUtc + contract.Purchase.Duration;
            var slotSize = Convert.ToInt64(contract.GetStatus()!.Request.Ask.SlotSize).Bytes();
            var expectedBalances = new Dictionary<EthAddress, TestToken>();

            foreach (var host in hosts) expectedBalances.Add(host.EthAddress, HostStartingBalance);
            foreach (var fill in fills)
            {
                var slotDuration = finishUtc - fill.SlotFilledEvent.Block.Utc;
                expectedBalances[fill.Host.EthAddress] += GetContractCostPerSlot(pricePerBytePerSecond, slotSize, slotDuration);
            }

            foreach (var pair in expectedBalances)
            {
                AssertTstBalance(pair.Key, pair.Value, $"Host {pair.Key} was not paid for storage.");

                Log($"Host {pair.Key} was paid for storage. Balance: {pair.Value}");
            }
        }

        protected void AssertHostsCollateralsAreUnchanged(IArchivistNodeGroup hosts)
        {
            // There is no separate collateral location yet.
            // All host balances should be equal to or greater than the starting balance.
            foreach (var host in hosts)
            {
                var retry = GetBalanceAssertRetry();
                retry.Run(() =>
                {
                    if (GetTstBalance(host) < HostStartingBalance)
                    {
                        throw new Exception(nameof(AssertHostsCollateralsAreUnchanged));
                    }
                });
            }
        }

        protected void WaitForContractStarted(IStoragePurchaseContract r)
        {
            try
            {
                r.WaitForStorageContractStarted();
            }
            catch
            {
                // Contract failed to start. Retrieve and log every call to ReserveSlot to identify which hosts
                // should have filled the slot.

                var requestId = r.PurchaseId.ToLowerInvariant();
                var calls = new List<ReserveSlotFunction>();
                GetContracts().GetEvents(GetTestRunTimeRange()).GetReserveSlotCalls(calls.Add);

                Log($"Request '{requestId}' failed to start. There were {calls.Count} hosts who called reserve-slot for it:");
                foreach (var c in calls)
                {
                    Log($" - {c.Block.Utc} Host: {c.FromAddress} RequestId: {c.RequestId.ToHex()} SlotIndex: {c.SlotIndex}");
                }
                throw;
            }
        }

        private ChainMonitor? SetupChainMonitor(ILog log, IGethNode gethNode, IArchivistContracts contracts, DateTime startUtc)
        {
            if (!MonitorChainState) return null;

            var result = new ChainMonitor(log, gethNode, contracts, this, startUtc, 
                updateInterval: TimeSpan.FromSeconds(3.0),
                monitorProofPeriods: MonitorProofPeriods);

            result.Start(() =>
            {
                Assert.Fail("Failure in chain monitor.");
            });
            return result;
        }

        private Retry GetBalanceAssertRetry()
        {
            return new Retry("AssertBalance",
                maxTimeout: TimeSpan.FromMinutes(10.0),
                sleepAfterFail: TimeSpan.FromSeconds(10.0),
                onFail: f => { },
                failFast: false);
        }

        private Retry GetBlockTTLAssertRetry()
        {
            return new Retry("AssertWithBlockTTLTimeout",
                maxTimeout: HostBlockTTL * 3,
                sleepAfterFail: HostBlockTTL / 3,
                onFail: f => { },
                failFast: false);
        }

        protected TestToken GetTstBalance(IArchivistNode node)
        {
            return GetContracts().GetTestTokenBalance(node);
        }

        protected TestToken GetTstBalance(EthAddress address)
        {
            return GetContracts().GetTestTokenBalance(address);
        }

        private Ether GetEthBalance(IArchivistNode node)
        {
            return GetGeth().GetEthBalance(node);
        }

        private Ether GetEthBalance(EthAddress address)
        {
            return GetGeth().GetEthBalance(address);
        }

        private TestToken GetContractFinalCost(TestToken pricePerBytePerSecond, IStoragePurchaseContract contract, IArchivistNodeGroup hosts)
        {
            var fills = GetOnChainSlotFills(hosts);
            var result = 0.Tst();
            var submitUtc = GetContractOnChainSubmittedUtc(contract);
            var finishUtc = submitUtc + contract.Purchase.Duration;
            var slotSize = Convert.ToInt64(contract.GetStatus()!.Request.Ask.SlotSize).Bytes();

            foreach (var fill in fills)
            {
                var slotDuration = finishUtc - fill.SlotFilledEvent.Block.Utc;
                result += GetContractCostPerSlot(pricePerBytePerSecond, slotSize, slotDuration);
            }

            return result;
        }

        private DateTime GetContractOnChainSubmittedUtc(IStoragePurchaseContract contract)
        {
            return Time.Retry(() =>
            {
                var events = GetContracts().GetEvents(GetTestRunTimeRange());
                var submitEvent = events.GetEvents<StorageRequestedEventDTO>().SingleOrDefault(e => e.RequestId.ToHex() == contract.PurchaseId);
                if (submitEvent == null)
                {
                    // We're too early.
                    throw new TimeoutException(nameof(GetContractOnChainSubmittedUtc) + "StorageRequest not found on-chain.");
                }
                return submitEvent.Block.Utc;
            }, nameof(GetContractOnChainSubmittedUtc));
        }

        private TestToken GetContractCostPerSlot(TestToken pricePerBytePerSecond, ByteSize slotSize, TimeSpan slotDuration)
        {
            var cost = pricePerBytePerSecond.TstWei * slotSize.SizeInBytes * (int)slotDuration.TotalSeconds;
            return cost.TstWei();
        }

        protected void AssertContractSlotsAreFilledByHosts(IStoragePurchaseContract contract, IArchivistNodeGroup hosts)
        {
            var activeHosts = new Dictionary<int, SlotFill>();

            Time.Retry(() =>
            {
                var fills = GetOnChainSlotFills(hosts, contract.PurchaseId);
                foreach (var fill in fills)
                {
                    var index = (int)fill.SlotFilledEvent.SlotIndex;
                    if (!activeHosts.ContainsKey(index))
                    {
                        activeHosts.Add(index, fill);
                    }
                }

                if (activeHosts.Count != contract.Purchase.MinRequiredNumberOfNodes) throw new Exception("Not all slots were filled...");

            }, nameof(AssertContractSlotsAreFilledByHosts));
        }

        protected void AssertContractIsOnChain(IStoragePurchaseContract contract)
        {
            // Check the creation event.
            AssertOnChainEvents(events =>
            {
                var onChainRequests = events.GetEvents<StorageRequestedEventDTO>();
                if (onChainRequests.Any(r => r.RequestId.ToHex() == contract.PurchaseId)) return;
                throw new Exception($"OnChain request {contract.PurchaseId} not found...");
            }, nameof(AssertContractIsOnChain));

            // Check that the getRequest call returns it.
            var rid = contract.PurchaseId.HexToByteArray();
            var cachedRequest = GetContracts().GetRequest(rid);
            if (cachedRequest == null) throw new Exception($"Failed to get Request from {nameof(GetRequestFunction)}");
            var r = cachedRequest.Request;
            Assert.That(r.Ask.Duration, Is.EqualTo(contract.Purchase.Duration.TotalSeconds));
            Assert.That(r.Ask.Slots, Is.EqualTo(contract.Purchase.MinRequiredNumberOfNodes));
            Assert.That(((int)r.Ask.ProofProbability), Is.EqualTo(contract.Purchase.ProofProbability));
        }

        protected void AssertOnChainEvents(Action<IArchivistContractsEvents> onEvents, string description)
        {
            Time.Retry(() =>
            {
                var events = GetContracts().GetEvents(GetTestRunTimeRange());
                onEvents(events);
            }, description);
        }

        protected TimeSpan CalculateContractFailTimespan()
        {
            var config = GetContracts().Deployment.Config;
            var requiredNumMissedProofs = Convert.ToInt32(config.Collateral.MaxNumberOfSlashes);
            var periodDuration = GetPeriodDuration();
            var gracePeriod = periodDuration;

            // Each host could miss 1 proof per period,
            // so the time we should wait is period time * requiredNum of missed proofs.
            // Except: the proof requirement has a concept of "downtime":
            // a segment of time where proof is not required.
            // We calculate the probability of downtime and extend the waiting
            // timeframe by a factor, such that all hosts are highly likely to have 
            // failed a sufficient number of proofs.

            float n = requiredNumMissedProofs;
            return gracePeriod + periodDuration * n * GetDowntimeFactor(config);
        }

        private float GetDowntimeFactor(MarketplaceConfig config)
        {
            byte numBlocksInDowntimeSegment = config.Proofs.Downtime;
            float downtime = numBlocksInDowntimeSegment;
            float window = 256.0f;
            var chanceOfDowntime = downtime / window;
            return 1.0f + (5.0f * chanceOfDowntime);
        }

        public class SlotFill
        {
            public SlotFill(SlotFilledEventDTO slotFilledEvent, IArchivistNode host)
            {
                SlotFilledEvent = slotFilledEvent;
                Host = host;
            }

            public SlotFilledEventDTO SlotFilledEvent { get; }
            public IArchivistNode Host { get; }

            public override string ToString()
            {
                return SlotFilledEvent.ToString();
            }
        }

        private class MarketplaceHandle
        {
            public MarketplaceHandle(IGethNode geth, IArchivistContracts contracts, ChainMonitor? chainMonitor)
            {
                Geth = geth;
                Contracts = contracts;
                ChainMonitor = chainMonitor;
            }

            public IGethNode Geth { get; }
            public IArchivistContracts Contracts { get; }
            public ChainMonitor? ChainMonitor { get; }
        }
    }
}
