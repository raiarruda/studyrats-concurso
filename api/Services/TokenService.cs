using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StudyRats.Api.Domain;

namespace StudyRats.Api.Services;

public class JwtOptions
{
    public string Key { get; set; } = "";
    public string Issuer { get; set; } = "studyrats";
    public string Audience { get; set; } = "studyrats";
    public int ExpiresDays { get; set; } = 30;
}

public class TokenService(IOptions<JwtOptions> options)
{
    private readonly JwtOptions _o = options.Value;

    public string Create(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_o.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("name", user.Name),
        };
        var token = new JwtSecurityToken(_o.Issuer, _o.Audience, claims,
            expires: DateTime.UtcNow.AddDays(_o.ExpiresDays), signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
