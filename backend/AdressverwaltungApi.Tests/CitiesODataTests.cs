using System.Net;
using System.Net.Http.Json;
using AdressverwaltungApi.Tests.Infrastructure;

namespace AdressverwaltungApi.Tests;

[Collection(ApiCollection.Name)]
public class CitiesODataTests : ODataTestBase
{
    public CitiesODataTests(ApiFactory factory) : base(factory) { }

    private static object NeueStadt(string postalCode, string cityName) => new { postalCode, cityName };

    [Fact]
    public async Task Get_OhneDaten_LiefertLeereListe()
    {
        Assert.Empty(await GetListAsync("/odata/Cities"));
    }

    [Fact]
    public async Task Post_ErstelltStadt()
    {
        var erstellt = await CreateAsync("Cities", NeueStadt("8001", "Zürich"));

        Assert.True(erstellt.GetProperty("id").GetInt32() > 0);
        Assert.Equal("8001", erstellt.GetProperty("postalCode").GetString());
        Assert.Equal("Zürich", erstellt.GetProperty("cityName").GetString());
    }

    [Fact]
    public async Task Post_OhneOrtsname_Liefert400()
    {
        var response = await Client.PostAsJsonAsync("/odata/Cities", NeueStadt("8001", ""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await GetListAsync("/odata/Cities"));
    }

    [Fact]
    public async Task GetByKey_LiefertStadt()
    {
        var erstellt = await CreateAsync("Cities", NeueStadt("3000", "Bern"));
        var id = erstellt.GetProperty("id").GetInt32();

        var stadt = await GetJsonAsync($"/odata/Cities({id})");

        Assert.Equal("Bern", stadt.GetProperty("cityName").GetString());
    }

    [Fact]
    public async Task GetByKey_UnbekannteId_Liefert404()
    {
        var response = await Client.GetAsync("/odata/Cities(999999)");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Entspricht der PLZ-Suche des Frontends (frontend/lib/api.ts)
    [Fact]
    public async Task Filter_PlzPraefix_LiefertSortierteTreffer()
    {
        await CreateAsync("Cities", NeueStadt("8400", "Winterthur"));
        await CreateAsync("Cities", NeueStadt("3000", "Bern"));
        await CreateAsync("Cities", NeueStadt("8001", "Zürich"));

        var staedte = await GetListAsync(
            "/odata/Cities?$filter=startswith(postalCode,'8')&$top=10&$orderby=postalCode");

        Assert.Equal(new[] { "8001", "8400" }, staedte.Select(s => s.GetProperty("postalCode").GetString()));
    }

    [Fact]
    public async Task Patch_AendertStadt()
    {
        var erstellt = await CreateAsync("Cities", NeueStadt("8001", "Zuerich"));
        var id = erstellt.GetProperty("id").GetInt32();

        var response = await Client.PatchAsJsonAsync($"/odata/Cities({id})", new { cityName = "Zürich" });

        Assert.True(response.IsSuccessStatusCode, $"Status: {response.StatusCode}");
        var stadt = await GetJsonAsync($"/odata/Cities({id})");
        Assert.Equal("Zürich", stadt.GetProperty("cityName").GetString());
        Assert.Equal("8001", stadt.GetProperty("postalCode").GetString());
    }

    [Fact]
    public async Task Patch_UnbekannteId_Liefert404()
    {
        var response = await Client.PatchAsJsonAsync("/odata/Cities(999999)", new { cityName = "X" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_LoeschtStadt()
    {
        var erstellt = await CreateAsync("Cities", NeueStadt("3000", "Bern"));
        var id = erstellt.GetProperty("id").GetInt32();

        var response = await Client.DeleteAsync($"/odata/Cities({id})");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await GetListAsync("/odata/Cities"));
    }

    [Fact]
    public async Task Delete_UnbekannteId_Liefert404()
    {
        var response = await Client.DeleteAsync("/odata/Cities(999999)");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
