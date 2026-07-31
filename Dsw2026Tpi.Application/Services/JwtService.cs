using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Dsw2026Tpi.Application.Services;

public class JwtService
{
    private readonly IConfiguration _configuration;
    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(string userId, string email, string role, long? dni = null)
    {
        IConfigurationSection jwtConfig = _configuration.GetSection("jxt");
        string keyText = jwtConfig["Key"] ?? throw new InvalidOperationException("jwt Key no configurada");
        string issuer = jwtConfig["Issuer"] ?? throw new InvalidOperationException("jwt Issuer no configurado");
        string audience = jwtConfig["Audience"] ?? throw new InvalidOperationException("jwt Audience no configurado");
        int expiresInMinutes = int.Parse(jwtConfig["ExpiresInMinutes"] ?? "60");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,  userId),
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, email),
            new(ClaimTypes.Role, role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        }; 

        if (dni.HasValue)
        {
            claims.Add(new Claim("dni", dni.Value.ToString())); 
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyText));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(expiresInMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token); 


    }
}
