using ArchNet.Domain.Entities;
using ArchNet.Domain.Enums;
using ArchNet.Features.Users.ListUsers;
using ArchNet.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace ArchNet.UnitTests.Features.Users;

public class ListUsersHandlerTests : IAsyncLifetime
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
    public async Task Handle_WithUsers_ReturnsPaginatedList()
    {
        _db.Users.AddRange(
            User.Create("Alice", "alice@qia.tech", "hash", "salt", UserRole.User),
            User.Create("Bob", "bob@qia.tech", "hash", "salt", UserRole.Manager),
            User.Create("Carol", "carol@qia.tech", "hash", "salt", UserRole.Admin));
        await _db.SaveChangesAsync();

        var handler = new ListUsersQueryHandler(_db);
        var result = await handler.Handle(new ListUsersQuery(Page: 1, PageSize: 2), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(3);
        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WithEmptyDatabase_ReturnsEmptyList()
    {
        var handler = new ListUsersQueryHandler(_db);
        var result = await handler.Handle(new ListUsersQuery(Page: 1, PageSize: 10), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(0);
        result.Value.Items.Should().BeEmpty();
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
