using ArchNet.Domain.Enums;
using ArchNet.Features.Users.CreateUser;
using ArchNet.Infrastructure.Persistence;
using DotNet.Testcontainers.Builders;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace ArchNet.UnitTests.Features.Users;

public class CreateUserHandlerTests : IAsyncLifetime
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
    public async Task Handle_WithValidCommand_CreatesUserAndReturnsResponse()
    {
        var handler = new CreateUserCommandHandler(_db, new CreateUserValidator());
        var command = new CreateUserCommand("Alice", "alice@qia.tech", "Secret123!", UserRole.User);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Alice");
        result.Value.Login.Should().Be("alice@qia.tech");
        result.Value.Role.Should().Be(UserRole.User);
    }

    [Fact]
    public async Task Handle_WithDuplicateLogin_ReturnsFailure()
    {
        var handler = new CreateUserCommandHandler(_db, new CreateUserValidator());
        var command = new CreateUserCommand("Alice", "alice@qia.tech", "Secret123!", UserRole.User);
        await handler.Handle(command, CancellationToken.None);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("User.LoginAlreadyExists");
    }

    [Fact]
    public async Task Handle_WithInvalidPassword_ReturnsFailure()
    {
        var handler = new CreateUserCommandHandler(_db, new CreateUserValidator());
        var command = new CreateUserCommand("Alice", "alice@qia.tech", "short", UserRole.User);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("User.ValidationFailed");
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
