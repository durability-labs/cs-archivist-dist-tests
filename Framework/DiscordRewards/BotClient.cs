using Logging;
using System.Net.Http.Json;
using Utils;

namespace DiscordRewards
{
    public interface IBotClient
    {
        Task<bool> IsOnline();
        Task<bool> SendRewards(EventsAndErrors command);
        Task EnsureBotOnline(CancellationToken ct);
    }

    public class BotClient : IBotClient
    {
        private readonly string host;
        private readonly int port;
        private readonly ILog log;
        private DateTime lastCheck = DateTime.MinValue;

        public BotClient(string host, int port, ILog log)
        {
            this.host = host;
            this.port = port;
            this.log = log;
        }

        public async Task<bool> IsOnline()
        {
            var result = await HttpGet();
            return result == "Pong";
        }

        public async Task EnsureBotOnline(CancellationToken ct)
        {
            var start = DateTime.UtcNow;
            var timeSince = start - lastCheck;
            if (timeSince.TotalSeconds < 30.0) return;

            while (!await IsOnline() && !ct.IsCancellationRequested)
            {
                await Task.Delay(5000);

                var elapsed = DateTime.UtcNow - start;
                if (elapsed.TotalMinutes > 10)
                {
                    var msg = "Unable to connect to bot for " + Time.FormatDuration(elapsed);
                    log.Error(msg);
                    throw new Exception(msg);
                }
            }

            lastCheck = start;
        }

        public async Task<bool> SendRewards(EventsAndErrors command)
        {
            if (command == null) return false;
            var result = await HttpPostJson(command);
            log.Log("Reward response: " + result);
            return result == "OK";
        }

        private async Task<string> HttpGet()
        {
            try
            {
                var client = new HttpClient();
                var response = await client.GetAsync(GetUrl());
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                log.Error(ex.ToString());
                return string.Empty;
            }
        }

        private async Task<string> HttpPostJson<T>(T body)
        {
            try
            {
                using var client = new HttpClient();
                using var content = JsonContent.Create(body);
                using var response = await client.PostAsync(GetUrl(), content);
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                log.Error(ex.ToString());
                return string.Empty;
            }
        }

        private string GetUrl()
        {
            return $"{host}:{port}/api/reward";
        }
    }

    public class DoNothingBotClient : IBotClient
    {
        public Task EnsureBotOnline(CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        public async Task<bool> IsOnline()
        {
            return true;
        }

        public async Task<bool> SendRewards(EventsAndErrors command)
        {
            return true;
        }
    }
}
