using System.Security.Cryptography;
using ArchNet.Common.ResultPattern;
using ArchNet.Domain.Entities;
using ArchNet.Features.Users.GetUser;
using ArchNet.Infrastructure.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ArchNet.Features.Users.CreateUser;

public sealed class CreateUserCommandHandler(AppDbContext db, CreateUserValidator validator)
    : ICommandHandler<CreateUserCommand, Result<GetUserResponse>>
{
    public async ValueTask<Result<GetUserResponse>> Handle(
        CreateUserCommand command, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<GetUserResponse>.Failure(
                new Error("User.ValidationFailed", validation.ToString()));

        var loginExists = await db.Users
            .AsNoTracking()
            .AnyAsync(u => u.Login == command.Login, cancellationToken);

        if (loginExists)
            return Result<GetUserResponse>.Failure(
                new Error("User.LoginAlreadyExists", "Login already taken."));

        var (hash, salt) = HashPassword(command.Password);
        var user = User.Create(command.Name, command.Login, hash, salt, command.Role);

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        return Result<GetUserResponse>.Success(
            new GetUserResponse(user.Id, user.Name, user.Login, user.Role));
    }

    private static (string hash, string salt) HashPassword(string password)
    {
        var saltBytes = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, 310_000, HashAlgorithmName.SHA256, 32);
        return (Convert.ToBase64String(hash), Convert.ToBase64String(saltBytes));
    }
}
