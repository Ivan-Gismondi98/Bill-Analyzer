using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BollettaAnalyzer.Application.Common.Interfaces;
using BollettaAnalyzer.Domain.Entities;
using Microsoft.IdentityModel.Tokens;

namespace BollettaAnalyzer.Infrastructure.Auth;

/// <summary>Impostazioni di firma del JWT (bind da appsettings: "Jwt").</summary>
public class JwtSettings
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "BollettaAnalyzer";
    public string Audience { get; set; } = "BollettaAnalyzer.Client";
    public int DurataOre { get; set; } = 8;
}

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtSettings _settings;

    public JwtTokenGenerator(JwtSettings settings) => _settings = settings;

    public (string token, DateTime scadenza) GeneraToken(Utente utente)
    {
        var scadenza = DateTime.UtcNow.AddHours(_settings.DurataOre);
        var chiave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var credenziali = new SigningCredentials(chiave, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, utente.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, utente.Email),
            new Claim(ClaimTypes.NameIdentifier, utente.Id.ToString()),
            new Claim("nome", utente.Nome),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: scadenza,
            signingCredentials: credenziali);

        return (new JwtSecurityTokenHandler().WriteToken(token), scadenza);
    }
}

public class PasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);
    public bool Verifica(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
