using System.Security.Cryptography;
using ArchNet.Common.ResultPattern;
using ArchNet.Features.Users.GetUser;
using ArchNet.Infrastructure.Persistence;
using Mediator;

namespace ArchNet.Features.Users.ChangePassword;

public sealed class ChangePasswordCommandHandler(AppDbContext db)
    : ICommandHandler<ChangePasswordCommand, Result<GetUserResponse>>
{
    public async ValueTask<Result<GetUserResponse>> Handle(
        ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await db.Users.FindAsync([command.UserId], cancellationToken);

        if (user is null)
            return Result<GetUserResponse>.Failure(
                new Error("User.NotFound", $"User '{command.UserId}' not found."));

        if (!VerifyPassword(command.CurrentPassword, user.PasswordHash, user.Salt))
            return Result<GetUserResponse>.Failure(
                new Error("User.InvalidPassword", "Current password is incorrect."));

        var (newHash, newSalt) = HashPassword(command.NewPassword);
        user.UpdatePassword(newHash, newSalt);
        await db.SaveChangesAsync(cancellationToken);

        return Result<GetUserResponse>.Success(
            new GetUserResponse(user.Id, user.Name, user.Login, user.Role));
    }

    private static bool VerifyPassword(string password, string storedHash, string storedSalt)
    {
        var saltBytes = Convert.FromBase64String(storedSalt);
        var computedHash = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, 310_000, HashAlgorithmName.SHA256, 32);
        var expectedHash = Convert.FromBase64String(storedHash);
        return CryptographicOperations.FixedTimeEquals(computedHash, expectedHash);
    }

    private static (string hash, string salt) HashPassword(string password)
    {
        var saltBytes = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, 310_000, HashAlgorithmName.SHA256, 32);
        return (Convert.ToBase64String(hash), Convert.ToBase64String(saltBytes));
    }
}
