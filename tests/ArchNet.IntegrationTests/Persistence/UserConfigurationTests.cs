using ArchNet.Domain.Entities;
using ArchNet.Domain.Enums;
using ArchNet.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace ArchNet.IntegrationTests.Persistence;

public class UserConfigurationTests : IAsyncLifetime
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
    public async Task Users_Table_CanPersistAndRetrieveUser()
    {
        var user = User.Create("Alice", "alice@qia.tech", "hash", "salt", UserRole.Admin);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var retrieved = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == user.Id);

        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("Alice");
        retrieved.Login.Should().Be("alice@qia.tech");
        retrieved.Role.Should().Be(UserRole.Admin);
        retrieved.PasswordHash.Should().Be("hash");
        retrieved.Salt.Should().Be("salt");
    }

    [Fact]
    public async Task Users_Login_IsUnique()
    {
        var user1 = User.Create("Alice", "alice@qia.tech", "hash", "salt", UserRole.User);
        var user2 = User.Create("Alice2", "alice@qia.tech", "hash2", "salt2", UserRole.User);
        _db.Users.Add(user1);
        await _db.SaveChangesAsync();

        _db.Users.Add(user2);
        var act = async () => await _db.SaveChangesAsync();

        await act.Should().ThrowAsync<Exception>();
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
