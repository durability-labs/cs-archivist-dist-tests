using Newtonsoft.Json;

namespace ArchivistWindowsStarter
{
    public class ConfigFile
    {
        public const string Filename = "config.json";

        public ConfigFile(string version, string network)
        {
            Version = version;
            Network = network;
        }

        public string Version { get; }
        public string Network { get; }

        public static ConfigFile? Load()
        {
            try
            {
                if (!File.Exists(Filename))
                {
                    var def = GetDefault();
                    Save(def);
                    return def;
                }
                var lines = File.ReadAllText(Filename);
                return JsonConvert.DeserializeObject<ConfigFile>(lines);
            }
            catch
            {
                return null;
            }
        }

        public static void Save(ConfigFile config)
        {
            File.WriteAllText(Filename, JsonConvert.SerializeObject(config));
        }

        private static ConfigFile GetDefault()
        {
            return new ConfigFile(version: "latest", network: "testnet");
        }
    }
}
