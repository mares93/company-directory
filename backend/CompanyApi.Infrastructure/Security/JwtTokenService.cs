using System.Text;
using CompanyApi.Application.Dtos;
using CompanyApi.Application.Services;
using CompanyApi.Domain;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CompanyApi.Infrastructure.Security;

public class JwtTokenService(IOptions<JwtOptions> options) : ITokenService
{
    public AuthResponse Create(User user)
    {
        var o = options.Value;
        var expires = DateTime.UtcNow.AddMinutes(o.ExpiresMinutes);
        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = o.Issuer,
            Audience = o.Audience,
            Expires = expires,
            Claims = new Dictionary<string, object> { ["sub"] = user.Id.ToString(), ["email"] = user.Email },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(o.Key)), SecurityAlgorithms.HmacSha256)
        });
        return new AuthResponse(token, user.Email, expires);
    }
}
