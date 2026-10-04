using AdressverwaltungApi.Models;

namespace AdressverwaltungApi.Services;

/// <summary>Stellt Bearer-Tokens für angemeldete Benutzer aus.</summary>
public interface ITokenService
{
    string CreateToken(User user);
}
