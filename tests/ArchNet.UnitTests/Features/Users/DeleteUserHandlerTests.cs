using ArchNet.Domain.Entities;
using ArchNet.Domain.Enums;
using ArchNet.Features.Users.DeleteUser;
using ArchNet.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace ArchNet.UnitTests.Features.Users;

public class DeleteUserHandlerTests : IAsyncLifetime
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
    public async Task Handle_WithExistingUser_DeletesAndReturnsId()
    {
        var user = User.Create("Alice", "alice@qia.tech", "hash", "salt", UserRole.User);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var handler = new DeleteUserCommandHandler(_db);
        var result = await handler.Handle(new DeleteUserCommand(user.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(user.Id);

        var deletedUser = await _db.Users.FindAsync(user.Id);
        deletedUser.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsFailure()
    {
        var handler = new DeleteUserCommandHandler(_db);
        var result = await handler.Handle(new DeleteUserCommand("nonexistent-id"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("User.NotFound");
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
