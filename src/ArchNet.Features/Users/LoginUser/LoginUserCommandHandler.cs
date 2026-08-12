using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ArchNet.Common.ResultPattern;
using ArchNet.Infrastructure.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ArchNet.Features.Users.LoginUser;

public sealed class LoginUserCommandHandler(
    AppDbContext db,
    LoginUserValidator validator,
    IConfiguration configuration)
    : ICommandHandler<LoginUserCommand, Result<LoginUserResponse>>
{
    public async ValueTask<Result<LoginUserResponse>> Handle(
        LoginUserCommand command, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<LoginUserResponse>.Failure(
                new Error("Login.ValidationFailed", validation.ToString()));

        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Login == command.Login, cancellationToken);

        if (user is null || !VerifyPassword(command.Password, user.PasswordHash, user.Salt))
            return Result<LoginUserResponse>.Failure(
                new Error("Login.InvalidCredentials", "Invalid login or password."));

        var token = GenerateToken(user.Id, user.Name, user.Role.ToString(), configuration);

        return Result<LoginUserResponse>.Success(
            new LoginUserResponse(token, user.Id, user.Name, user.Role));
    }

    private static bool VerifyPassword(string password, string storedHash, string storedSalt)
    {
        var saltBytes    = Convert.FromBase64String(storedSalt);
        var computedHash = Rfc2898DeriveBytes.Pbkdf2(
            password, saltBytes, 310_000, HashAlgorithmName.SHA256, 32);
        var expectedHash = Convert.FromBase64String(storedHash);
        return CryptographicOperations.FixedTimeEquals(computedHash, expectedHash);
    }

    private static string GenerateToken(string userId, string name, string role, IConfiguration cfg)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg["Jwt:Key"]!))
        {
            KeyId = cfg["Jwt:KeyId"] ?? "arch-net"
        };
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name,           name),
            new Claim(ClaimTypes.Role,           role)
        };
        var token = new JwtSecurityToken(
            issuer:             cfg["Jwt:Issuer"],
            audience:           cfg["Jwt:Audience"],
            claims:             claims,
            expires:            DateTime.UtcNow.AddMinutes(double.Parse(cfg["Jwt:ExpiresInMinutes"]!)),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
