using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace StudyRats.Api.Controllers;

[ApiController]
public abstract class AppControllerBase : ControllerBase
{
    protected Guid UserId =>
        Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                   ?? throw new InvalidOperationException("Token sem 'sub'."));

    protected ObjectResult Fail(int status, string detail) => Problem(detail: detail, statusCode: status);
}
