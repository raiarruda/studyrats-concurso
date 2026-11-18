using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using StudyRats.Api.Data;
using StudyRats.Api.Domain;
using StudyRats.Api.Dtos;
using StudyRats.Api.Services;

namespace StudyRats.Api.Controllers;

[Route("api/auth")]
[EnableRateLimiting("auth")]
public class AuthController(AppDbContext db, TokenService tokens) : AppControllerBase
{
    // Hash válido de uma senha qualquer: usado para gastar o mesmo tempo quando o e-mail não existe.
    private static readonly string DummyHash = BCrypt.Net.BCrypt.HashPassword("dummy-password-for-timing");

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest req, CancellationToken ct)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email, ct))
            return Fail(409, "Este e-mail já está cadastrado.");

        var user = new User
        {
            Name = req.Name.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
        };
        db.Users.Add(user);

        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException) // corrida entre dois cadastros com o mesmo e-mail
        {
            return Fail(409, "Este e-mail já está cadastrado.");
        }

        return Ok(new AuthResponse(tokens.Create(user), new UserDto(user.Id, user.Name, user.Email)));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest req, CancellationToken ct)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        var ok = BCrypt.Net.BCrypt.Verify(req.Password, user?.PasswordHash ?? DummyHash);
        if (user is null || !ok)
            return Fail(401, "E-mail ou senha inválidos.");

        return Ok(new AuthResponse(tokens.Create(user), new UserDto(user.Id, user.Name, user.Email)));
    }
}
