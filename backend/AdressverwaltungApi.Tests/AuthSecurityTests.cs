using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AdressverwaltungApi.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AdressverwaltungApi.Tests;

/// <summary>Sicherheitsrelevantes Verhalten von /auth und den Audit-Feldern.</summary>
[Collection(ApiCollection.Name)]
public class AuthSecurityTests : ODataTestBase
{
    public AuthSecurityTests(ApiFactory factory) : base(factory) { }

    private static object NeuerBenutzer(string email) => new
    {
        email,
        password    = "Ein-Sicheres-Passwort-1!",
        displayName = "Neuer Benutzer",
    };

    [Fact]
    public async Task Register_OhneToken_Liefert401_UndLegtKeinenBenutzerAn()
    {
        using var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/register", NeuerBenutzer("anonym@example.com"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await Factory.WithDbContextAsync(async db =>
            Assert.False(await db.Users.AnyAsync(u => u.Email == "anonym@example.com")));
    }

    [Fact]
    public async Task Register_MitToken_AntwortetFuerNeueUndBestehendeEmailGleich()
    {
        var email = $"neu-{Guid.NewGuid():N}@example.com";

        var erste  = await Client.PostAsJsonAsync("/auth/register", NeuerBenutzer(email));
        var zweite = await Client.PostAsJsonAsync("/auth/register", NeuerBenutzer(email));

        Assert.Equal(HttpStatusCode.OK, erste.StatusCode);
        Assert.Equal(HttpStatusCode.OK, zweite.StatusCode);
        Assert.Equal(await erste.Content.ReadAsStringAsync(), await zweite.Content.ReadAsStringAsync());
        await Factory.WithDbContextAsync(async db =>
            Assert.Equal(1, await db.Users.CountAsync(u => u.Email == email)));
    }

    [Theory]
    [InlineData(ApiFactory.TestUserEmail, "falsches-passwort")]
    [InlineData("unbekannt@example.com", "falsches-passwort")]
    public async Task Login_MitFalschenDaten_Liefert401(string email, string password)
    {
        using var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/login", new { email, password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_MitXUserHeader_SchreibtTrotzdemDenBenutzerAusDemToken()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/odata/Cities")
        {
            Content = JsonContent.Create(new { postalCode = "8001", cityName = "Zürich" }),
        };
        request.Headers.Add("X-User", "gefaelscht");

        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(ApiFactory.TestUserName, body.GetProperty("createdBy").GetString());
    }
}
