using System.Net;
using AdressverwaltungApi.Tests.Infrastructure;

namespace AdressverwaltungApi.Tests;

[Collection(ApiCollection.Name)]
public class MetadataTests : ODataTestBase
{
    public MetadataTests(ApiFactory factory) : base(factory) { }

    [Fact]
    public async Task Metadata_EnthaeltBeideEntitySets()
    {
        var response = await Client.GetAsync("/odata/$metadata");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var xml = await response.Content.ReadAsStringAsync();
        Assert.Contains("EntitySet Name=\"Adressen\"", xml);
        Assert.Contains("EntitySet Name=\"Cities\"", xml);
    }

    [Fact]
    public async Task Metadata_PropertiesSindCamelCase()
    {
        var xml = await Client.GetStringAsync("/odata/$metadata");

        Assert.Contains("Property Name=\"vorname\"", xml);
        Assert.Contains("Property Name=\"postalCode\"", xml);
    }

    [Fact]
    public async Task ServiceDocument_ListetBeideEntitySets()
    {
        var entitySets = (await GetListAsync("/odata"))
            .Select(e => e.GetProperty("name").GetString())
            .ToList();

        Assert.Contains("Adressen", entitySets);
        Assert.Contains("Cities", entitySets);
    }
}
