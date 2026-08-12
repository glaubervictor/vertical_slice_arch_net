using ArchNet.Domain.Entities;
using ArchNet.Domain.Enums;
using ArchNet.Features.Users.UpdateUser;
using ArchNet.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace ArchNet.UnitTests.Features.Users;

public class UpdateUserHandlerTests : IAsyncLifetime
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
    public async Task Handle_WithValidCommand_UpdatesUserAndReturnsResponse()
    {
        var user = User.Create("Alice", "alice@qia.tech", "hash", "salt", UserRole.User);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var handler = new UpdateUserCommandHandler(_db);
        var command = new UpdateUserCommand(user.Id, "Alice Smith", UserRole.Manager);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Alice Smith");
        result.Value.Role.Should().Be(UserRole.Manager);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsFailure()
    {
        var handler = new UpdateUserCommandHandler(_db);
        var command = new UpdateUserCommand("nonexistent-id", "Alice", UserRole.User);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("User.NotFound");
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
