using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AdressverwaltungApi.Tests.Infrastructure;

namespace AdressverwaltungApi.Tests;

/// <summary>Die OData-Endpunkte sind nur mit gültigem JWT erreichbar.</summary>
[Collection(ApiCollection.Name)]
public class AuthorizationTests
{
    private readonly ApiFactory _factory;

    public AuthorizationTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/odata/Adressen")]
    [InlineData("/odata/Adressen(1)")]
    [InlineData("/odata/Cities")]
    [InlineData("/odata/Cities(1)")]
    [InlineData("/odata")]
    [InlineData("/odata/$metadata")]
    [InlineData("/settings")]
    public async Task Get_OhneToken_Liefert401(string url)
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/odata/Adressen")]
    [InlineData("/odata/Cities")]
    public async Task Post_OhneToken_Liefert401(string url)
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(url, new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_MitUngueltigemToken_Liefert401()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "kein.gueltiges.token");

        var response = await client.GetAsync("/odata/Adressen");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_MitGueltigemToken_Liefert200()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/odata/Adressen");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
