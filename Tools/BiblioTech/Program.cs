using ArchivistNetworkConfig;
using ArgsUniform;
using BiblioTech.ArchivistChecking;
using BiblioTech.Commands;
using BiblioTech.Rewards;
using Discord;
using Discord.WebSocket;
using Logging;

namespace BiblioTech
{
    public class Program
    {
        private DiscordSocketClient client = null!;
        private CustomReplacement replacement = null!;

        public static CallDispatcher Dispatcher { get; private set; } = null!;
        public static Configuration Config { get; private set; } = null!;
        public static UserRepo UserRepo { get; } = new UserRepo();
        public static AdminChecker AdminChecker { get; private set; } = null!;
        public static IDiscordRoleDriver RoleDriver { get; set; } = null!;
        public static ChainActivityHandler ChainActivityHandler { get; set; } = null!;
        public static ChainEventsSender EventsSender { get; set; } = null!;
        public static ILog Log { get; private set; } = null!;
        public static GethLink? GethLink { get; private set; } = null;

        public static Task Main(string[] args)
        {
            var uniformArgs = new ArgsUniform<Configuration>(PrintHelp, args);
            Config = uniformArgs.Parse();

            Log = new LogSplitter(
                new FileLog(Path.Combine(Config.LogPath, "discordbot")),
                new ConsoleLog()
            );

            GethLink = GethLink.Create();

            Dispatcher = new CallDispatcher(Log);

            EnsurePath(Config.DataPath);
            EnsurePath(Config.UserDataPath);
            EnsurePath(Config.EndpointsPath);
            EnsurePath(Config.ChecksDataPath);

            return new Program().MainAsync(args);
        }

        public async Task MainAsync(string[] args)
        {
            Log.Log("Starting Archivist Discord Bot...");
            try
            {
                replacement = new CustomReplacement(Config);
                replacement.Load();

                LoadReplacementsFromNetworkConfig(replacement);
            }
            catch (Exception ex)
            {
                Log.Error("Failed to load logReplacements: " + ex);
                throw;
            }

            if (Config.DebugNoDiscord)
            {
                Log.Log("Debug option is set. Discord connection disabled!");
                RoleDriver = new LoggingRoleDriver(Log);
            }
            else
            {
                await StartDiscordBot();
            }

            var builder = WebApplication.CreateBuilder(args);
            builder.WebHost.ConfigureKestrel((context, options) =>
            {
                options.ListenAnyIP(Config.RewardApiPort);
            });
            builder.Services.AddControllers();
            var app = builder.Build();
            app.MapControllers();

            Log.Log("Running...");
            await app.RunAsync();
            await Task.Delay(-1);
        }

        private async Task StartDiscordBot()
        {
            client = new DiscordSocketClient();
            client.Log += ClientLog;

            var checkRepo = new CheckRepo(Log, Config);
            var archivistWrapper = new ArchivistWrapper(Log, Config);
            var checker = new ArchivistTwoWayChecker(Log, Config, checkRepo, archivistWrapper);
            var notifyCommand = new NotifyCommand();
            var associateCommand = new UserAssociateCommand(notifyCommand);
            var roleRemover = new ActiveP2pRoleRemover(Config, Log, checkRepo);
            var handler = new CommandHandler(Log, client, replacement, roleRemover,
                new GetBalanceCommand(associateCommand),
                new MintCommand(associateCommand),
                associateCommand,
                notifyCommand,
                new CheckUploadCommand(checker),
                new CheckDownloadCommand(checker),
                new AdminCommand(replacement)
            );

            await client.LoginAsync(TokenType.Bot, Config.ApplicationToken);
            await client.StartAsync();
            AdminChecker = new AdminChecker();
        }

        private static void PrintHelp()
        {
            Log.Log("BiblioTech - Archivist Discord Bot");
        }

        private Task ClientLog(LogMessage msg)
        {
            Log.Log("DiscordClient: " + msg.ToString());
            return Task.CompletedTask;
        }

        private static void EnsurePath(string path)
        {
            if (Directory.Exists(path)) return;
            Directory.CreateDirectory(path);
        }

        private void LoadReplacementsFromNetworkConfig(CustomReplacement replacement)
        {
            var connector = new ArchivistNetworkConnector();
            var config = connector.GetConfig();
            var r = config.Team.GetNodesAsLogReplacements();
            foreach (var pair in r)
            {
                replacement.Add(pair.Key, pair.Value);
            }
        }
    }
}
