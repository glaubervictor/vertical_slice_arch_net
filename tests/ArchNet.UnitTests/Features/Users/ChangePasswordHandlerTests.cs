using System.Security.Cryptography;
using ArchNet.Domain.Entities;
using ArchNet.Domain.Enums;
using ArchNet.Features.Users.ChangePassword;
using ArchNet.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace ArchNet.UnitTests.Features.Users;

public class ChangePasswordHandlerTests : IAsyncLifetime
{
    private AppDbContext _db = null!;
    private PostgreSqlContainer _postgres = null!;

    public async Task InitializeAsync()
    {
        _postgres = new PostgreSqlBuilder("postgres:16").Build();
        await _postgres.StartAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        _db = new AppDbContext(options);
        await _db.Database.EnsureCreatedAsync();
    }

    [Fact]
    public async Task Handle_WithValidCurrentPassword_ChangesPassword()
    {
        var (hash, salt) = HashPassword("OldPassword123!");
        var user = User.Create("Alice", "alice@qia.tech", hash, salt, UserRole.User);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var handler = new ChangePasswordCommandHandler(_db);
        var command = new ChangePasswordCommand(user.Id, "OldPassword123!", "NewPassword456!");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task Handle_WithWrongCurrentPassword_ReturnsFailure()
    {
        var (hash, salt) = HashPassword("RealPassword123!");
        var user = User.Create("Alice", "alice@qia.tech", hash, salt, UserRole.User);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var handler = new ChangePasswordCommandHandler(_db);
        var command = new ChangePasswordCommand(user.Id, "WrongPassword!", "NewPassword456!");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("User.InvalidPassword");
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsFailure()
    {
        var handler = new ChangePasswordCommandHandler(_db);
        var command = new ChangePasswordCommand("nonexistent-id", "any", "any");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("User.NotFound");
    }

    private static (string hash, string salt) HashPassword(string password)
    {
        var saltBytes = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, 310_000, HashAlgorithmName.SHA256, 32);
        return (Convert.ToBase64String(hash), Convert.ToBase64String(saltBytes));
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
