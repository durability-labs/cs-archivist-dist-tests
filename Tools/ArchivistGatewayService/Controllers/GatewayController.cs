using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace ArchivistGatewayService.Controllers;

[ApiController]
public class GatewayController : ControllerBase
{
    private readonly HttpClient client = new HttpClient();
    private readonly NodeSelector selector;

    public GatewayController(NodeSelector selector, Configuration config)
    {
        this.selector = selector;

        client.Timeout = TimeSpan.FromMinutes(config.RequestTimeoutMinutes);
    }

    [HttpGet()]
    [Route("manifest/{cid}")]
    public async Task GetManifest(string cid)
    {
        var nodeUrl = selector.GetNodeUrl(cid);
        var sourceUrl = $"{nodeUrl}data/{cid}/network/manifest";

        await StreamResponse(sourceUrl, MediaTypeWithQualityHeaderValue.Parse("application/json"));
    }

    [HttpGet()]
    [Route("data/{cid}")]
    public async Task GetData(string cid)
    {
        var nodeUrl = selector.GetNodeUrl(cid);
        var sourceUrl = $"{nodeUrl}data/{cid}/network/stream";

        await StreamResponse(sourceUrl, MediaTypeWithQualityHeaderValue.Parse("application/octet-stream"));
    }

    private async Task StreamResponse(string sourceUrl, MediaTypeWithQualityHeaderValue acceptHeader)
    {
        using var request = new HttpRequestMessage();
        request.Method = new HttpMethod("GET");
        request.Headers.Accept.Add(acceptHeader);
        request.RequestUri = new Uri(sourceUrl, UriKind.Absolute);

        var clientResponse = await client.SendAsync(request);
        var status = clientResponse.StatusCode;

        var responseStream = await clientResponse.Content.ReadAsStreamAsync();
        await responseStream.CopyToAsync(Response.Body);
    }
}
