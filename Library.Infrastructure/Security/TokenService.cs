using Library.Application.Interfaces;
using Library.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Library.Infrastructure.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public TokenResult GenerateToken(Member member)
    {
        var issuer = _configuration["Jwt:Issuer"]!;
        var audience = _configuration["Jwt:Audience"]!;
        var signingKey = _configuration["Jwt:SigningKey"]!;
        var expiryMinutes =
            int.Parse(_configuration["Jwt:ExpiryMinutes"]!);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, member.Email.Value),

            new(
                ClaimTypes.NameIdentifier,
                member.Id.ToString()),

            new(
                ClaimTypes.Email,
                member.Email.Value),

            new(
                ClaimTypes.Role,
                member.Role.ToString())
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(signingKey));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: expiresAt,
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler()
            .WriteToken(token);

        return new TokenResult(tokenString, expiresAt);
    }
}