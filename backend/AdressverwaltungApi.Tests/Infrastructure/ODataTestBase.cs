using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace AdressverwaltungApi.Tests.Infrastructure;

/// <summary>
/// Basisklasse für OData-Tests: leert vor jedem Test die Datenbank und
/// stellt einen angemeldeten HTTP-Client bereit.
/// </summary>
public abstract class ODataTestBase : IAsyncLifetime
{
    protected ApiFactory Factory { get; }
    protected HttpClient Client { get; private set; } = null!;

    protected ODataTestBase(ApiFactory factory)
    {
        Factory = factory;
    }

    public async Task InitializeAsync()
    {
        Client = await Factory.CreateAuthenticatedClientAsync();
        await Factory.ResetDataAsync();
    }

    public Task DisposeAsync()
    {
        Client.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>POST auf ein EntitySet; erwartet 201 und gibt die erstellte Entität zurück.</summary>
    protected async Task<JsonElement> CreateAsync(string entitySet, object body)
    {
        var response = await Client.PostAsJsonAsync($"/odata/{entitySet}", body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    /// <summary>GET; erwartet 200 und gibt den JSON-Body zurück.</summary>
    protected async Task<JsonElement> GetJsonAsync(string url)
    {
        var response = await Client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    /// <summary>GET auf eine Collection; gibt die Einträge aus "value" zurück.</summary>
    protected async Task<List<JsonElement>> GetListAsync(string url)
    {
        var body = await GetJsonAsync(url);
        return body.GetProperty("value").EnumerateArray().ToList();
    }

    protected static object NeueAdresse(
        string vorname = "Max",
        string name = "Muster",
        string ort = "Baar",
        string plz = "6340") => new
    {
        vorname,
        name,
        strasse        = "Dorfstrasse",
        strassennummer = "99",
        plz,
        ort,
    };
}
