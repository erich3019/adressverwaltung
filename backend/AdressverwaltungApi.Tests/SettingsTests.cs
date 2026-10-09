using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AdressverwaltungApi.Models;
using AdressverwaltungApi.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AdressverwaltungApi.Tests;

/// <summary>Einstellungen: Benachrichtigungs-E-Mail, Akzentfarbe und das Recht zum Ändern.</summary>
[Collection(ApiCollection.Name)]
public class SettingsTests : ODataTestBase
{
    public SettingsTests(ApiFactory factory) : base(factory) { }

    [Fact]
    public async Task Get_OhneGespeicherteEinstellungen_LiefertStandardwerte()
    {
        var settings = await GetJsonAsync("/settings");

        Assert.Equal(string.Empty, settings.GetProperty("notificationEmail").GetString());
        Assert.Equal(AccentColors.Default, settings.GetProperty("accentColor").GetString());
    }

    [Fact]
    public async Task Get_MeldetObDerBenutzerAendernDarf()
    {
        using var user = await Factory.CreateClientForNewUserAsync(Roles.User);

        var alsAdmin = await GetJsonAsync("/settings");
        var alsUser  = await user.GetFromJsonAsync<JsonElement>("/settings");

        Assert.True(alsAdmin.GetProperty("canEdit").GetBoolean());
        Assert.False(alsUser.GetProperty("canEdit").GetBoolean());
    }

    [Fact]
    public async Task Put_SpeichertEmailUndFarbe()
    {
        var response = await Client.PutAsJsonAsync("/settings",
            new { notificationEmail = " neu@example.com ", accentColor = "green" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var settings = await GetJsonAsync("/settings");
        Assert.Equal("neu@example.com", settings.GetProperty("notificationEmail").GetString());
        Assert.Equal("green", settings.GetProperty("accentColor").GetString());
    }

    [Fact]
    public async Task Put_OhneFarbe_LaesstDieFarbeUnveraendert()
    {
        await Client.PutAsJsonAsync("/settings", new { notificationEmail = "a@example.com", accentColor = "teal" });

        var response = await Client.PutAsJsonAsync("/settings", new { notificationEmail = "b@example.com" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var settings = await GetJsonAsync("/settings");
        Assert.Equal("b@example.com", settings.GetProperty("notificationEmail").GetString());
        Assert.Equal("teal", settings.GetProperty("accentColor").GetString());
    }

    [Fact]
    public async Task Put_MitLeererEmail_SchaltetDieBenachrichtigungAus()
    {
        await Client.PutAsJsonAsync("/settings", new { notificationEmail = "a@example.com" });

        var response = await Client.PutAsJsonAsync("/settings", new { notificationEmail = "" });
        await CreateAsync("Adressen", NeueAdresse());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(Factory.Emails.Sent);
    }

    [Theory]
    [InlineData("keine-adresse", null)]
    [InlineData("a@example.com", "pink")]
    [InlineData("a@example.com", "")]
    public async Task Put_MitUngueltigenWerten_Liefert400_UndSpeichertNichts(string email, string? farbe)
    {
        var response = await Client.PutAsJsonAsync("/settings", new { notificationEmail = email, accentColor = farbe });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await Factory.WithDbContextAsync(async db => Assert.False(await db.Settings.AnyAsync()));
    }
}
