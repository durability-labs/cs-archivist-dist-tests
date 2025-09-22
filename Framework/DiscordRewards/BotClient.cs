using Logging;
using System.Net.Http.Json;

namespace DiscordRewards
{
    public class BotClient
    {
        private readonly string host;
        private readonly int port;
        private readonly ILog log;

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
}
