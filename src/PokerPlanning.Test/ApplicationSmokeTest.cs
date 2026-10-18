using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PokerPlanning.Test;

public class ApplicationSmokeTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApplicationSmokeTest(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    [Fact]
    public async Task HomeRendersAndBlazorNegotiates()
    {
        var home = await _client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, home.StatusCode);
        Assert.Contains("_framework/blazor.server.js", await home.Content.ReadAsStringAsync());
        var negotiate = await _client.PostAsync("/_blazor/negotiate?negotiateVersion=1", null);
        Assert.Equal(HttpStatusCode.OK, negotiate.StatusCode);
        Assert.Contains("WebSockets", await negotiate.Content.ReadAsStringAsync());
    }
}
