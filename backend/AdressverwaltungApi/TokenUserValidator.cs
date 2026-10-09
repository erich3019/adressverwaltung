using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using AdressverwaltungApi.Data;
using AdressverwaltungApi.Services;

namespace AdressverwaltungApi;

/// <summary>
/// Gleicht ein gültig signiertes Token mit der Datenbank ab:
/// - Den Benutzer muss es noch geben.
/// - Die Token-Version muss stimmen (nach einer Abmeldung tut sie das nicht mehr).
/// - Die Rolle kommt aus der Datenbank, nicht aus dem Token.
/// Kostet eine kleine Abfrage pro Anfrage, dafür wirken Abmeldung, Löschung und
/// Rollenwechsel sofort und nicht erst nach Ablauf des Tokens.
/// </summary>
public static class TokenUserValidator
{
    public static async Task ValidateAsync(TokenValidatedContext context)
    {
        var principal = context.Principal;

        if (!int.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            context.Fail("Token ohne Benutzer-Id.");
            return;
        }

        var db   = context.HttpContext.RequestServices.GetRequiredService<AdresseDbContext>();
        var user = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.Role, u.TokenVersion })
            .FirstOrDefaultAsync(context.HttpContext.RequestAborted);

        // Tokens aus der Zeit vor dem Widerruf tragen keine Version und gelten als Version 0
        _ = int.TryParse(principal!.FindFirstValue(JwtTokenService.TokenVersionClaim), out var tokenVersion);

        if (user is null || user.TokenVersion != tokenVersion)
        {
            context.Fail("Token widerrufen oder Benutzer nicht mehr vorhanden.");
            return;
        }

        ((ClaimsIdentity)principal.Identity!).AddClaim(new Claim(ClaimTypes.Role, user.Role));
    }
}
