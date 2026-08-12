using ArchNet.Domain.Entities;
using ArchNet.Domain.Enums;
using ArchNet.Features.Users.GetUser;
using ArchNet.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace ArchNet.UnitTests.Features.Users;

public class GetUserHandlerTests : IAsyncLifetime
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
    public async Task Handle_WhenUserExists_ReturnsUser()
    {
        var user = User.Create("Bob", "bob@qia.tech", "hash", "salt", UserRole.Manager);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var handler = new GetUserQueryHandler(_db);
        var result = await handler.Handle(new GetUserQuery(user.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Bob");
        result.Value.Login.Should().Be("bob@qia.tech");
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsFailure()
    {
        var handler = new GetUserQueryHandler(_db);
        var result = await handler.Handle(new GetUserQuery("nonexistent-id"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("User.NotFound");
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
