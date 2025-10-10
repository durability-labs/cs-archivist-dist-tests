using System.Threading;
using System;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace ArchivistGatewayService.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;

    public WeatherForecastController(ILogger<WeatherForecastController> logger)
    {
        _logger = logger;
    }

    private readonly HttpClient client = new HttpClient();

    [HttpGet()]
    public async Task Forward()
    {
        var sourceUrl = "http://192.168.178.26:8081/api/archivist/v1/data/zDvZRwzkxe14JRMgyMirxUN5U363bzwfRRoqtK6Bw89uSev4AAxX/network/stream";

        var cts = new CancellationTokenSource();
        var ct = cts.Token;

        using (var request = new HttpRequestMessage())
        {
            request.Method = new HttpMethod("GET");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/octet-stream"));
            request.RequestUri = new Uri(sourceUrl, UriKind.Absolute);

            var clientResponse = await client.SendAsync(request, ct);
            var status = clientResponse.StatusCode;

            var responseStream = await clientResponse.Content.ReadAsStreamAsync(ct);
            await responseStream.CopyToAsync(Response.Body);
        }

    }
}
