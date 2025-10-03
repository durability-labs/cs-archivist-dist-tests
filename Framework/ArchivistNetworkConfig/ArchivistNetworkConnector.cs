using Newtonsoft.Json;
using Utils;

namespace ArchivistNetworkConfig
{
    public class ArchivistNetworkConnector
    {
        private readonly string network;
        private readonly string version;
        private ArchivistNetwork? model = null;

        public ArchivistNetworkConnector()
            : this(
                EnvVar.GetOrDefault("ARCHIVIST_NETWORK", "testnet"),
                EnvVar.GetOrDefault("ARCHIVIST_VERSION", "latest")
            )
        {
        }

        public ArchivistNetworkConnector(string network, string version)
        {
            this.network = network.ToLowerInvariant();
            this.version = version.ToLowerInvariant();
        }

        public ArchivistNetwork GetConfig()
        {
            if (model == null)
            {
                var retry = new Retry(nameof(FetchModel),
                    maxTimeout: TimeSpan.FromMinutes(5.0),
                    sleepAfterFail: TimeSpan.FromSeconds(10.0),
                    onFail: f => { },
                    failFast: false);

                var fullModel = retry.Run(FetchModel);
                model = MapToVersion(fullModel);
            }
            return model;
        }

        private NetworkConfig FetchModel()
        {
            using var client = new HttpClient();
            var response = Time.Wait(client.GetAsync($"https://config.archivist.storage/{network}.json"));
            var str = Time.Wait(response.Content.ReadAsStringAsync());
            return JsonConvert.DeserializeObject<NetworkConfig>(str)!;
        }

        private ArchivistNetwork MapToVersion(NetworkConfig fullModel)
        {
            var selected = GetVersion(fullModel);

            return new ArchivistNetwork
            {
                Version = fullModel.Archivist.Single(v => v.Version == selected),
                SPR = fullModel.SPRs.First(s => s.SupportedVersions.Contains(selected)),
                RPCs = fullModel.RPCs,
                Marketplace = fullModel.Marketplace.First(m => m.SupportedVersions.Contains(selected)),
                Team = MapToVersion(fullModel.Team, selected)
            };
        }

        private TeamObject MapToVersion(ArchivistNetworkTeamObject team, string selected)
        {
            return new TeamObject
            {
                Nodes = MapToVersion(team.Nodes, selected),
                Utils = team.Utils
            };
        }

        private TeamNodesCategory[] MapToVersion(ArchivistNetworkTeamNodesEntry[] nodes, string selected)
        {
            return nodes.Select(n => MapToVersion(n, selected)).ToArray();
        }

        private TeamNodesCategory MapToVersion(ArchivistNetworkTeamNodesEntry n, string selected)
        {
            return new TeamNodesCategory
            {
                Category = n.Category,
                Instances = n.Versions.Single(v => v.Version == selected).Instances
            };
        }

        private string GetVersion(NetworkConfig fullModel)
        {
            if (version == "latest")
            {
                return fullModel.Latest;
            }
            return version;
        }
    }
}
