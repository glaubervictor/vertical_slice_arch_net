using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using ArchNet.Api;
using ArchNet.Domain.Entities;
using ArchNet.Domain.Enums;
using ArchNet.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace ArchNet.IntegrationTests.Features.Users;

public class UserGraphQLTests : IClassFixture<UserGraphQLTests.ApiFixture>
{
    private readonly ApiFixture _fixture;

    public UserGraphQLTests(ApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateUser_Mutation_ReturnsCreatedUser()
    {
        var client = _fixture.CreateAuthenticatedClient();
        var mutation = """
            mutation {
                users {
                    createUser(input: {
                        name: "Alice"
                        login: "alice@graphql.test"
                        password: "Secret123!"
                        role: User
                    }) {
                        id name login role
                    }
                }
            }
            """;

        var response = await client.PostAsJsonAsync("/graphql", new { query = mutation });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var createUser = body.GetProperty("data").GetProperty("users").GetProperty("createUser");
        createUser.GetProperty("name").GetString().Should().Be("Alice");
        createUser.GetProperty("login").GetString().Should().Be("alice@graphql.test");
        createUser.GetProperty("role").GetString().Should().Be("User");
        createUser.GetProperty("id").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetUser_Query_ReturnsUser()
    {
        var client = _fixture.CreateAuthenticatedClient();

        var createMutation = """
            mutation {
                users {
                    createUser(input: {
                        name: "Bob"
                        login: "bob@graphql.test"
                        password: "Secret123!"
                        role: Manager
                    }) { id }
                }
            }
            """;
        var createResponse = await client.PostAsJsonAsync("/graphql", new { query = createMutation });
        createResponse.EnsureSuccessStatusCode();
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = createBody.GetProperty("data").GetProperty("users").GetProperty("createUser").GetProperty("id").GetString();

        var query = $$"""
            query {
                users {
                    user(id: "{{id}}") {
                        id name login role
                    }
                }
            }
            """;
        var response = await client.PostAsJsonAsync("/graphql", new { query });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").GetProperty("users").GetProperty("user").GetProperty("name").GetString().Should().Be("Bob");
    }

    [Fact]
    public async Task ListUsers_Query_ReturnsPaginatedResults()
    {
        var client = _fixture.CreateAuthenticatedClient();
        var query = """
            query {
                users {
                    users(page: 1, pageSize: 10) {
                        total
                        items { id name login role }
                    }
                }
            }
            """;

        var response = await client.PostAsJsonAsync("/graphql", new { query });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").GetProperty("users").GetProperty("users").GetProperty("total").GetInt32().Should().BeGreaterThanOrEqualTo(0);
    }

    public class ApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private const string AdminLogin    = "admin@integration.test";
        private const string AdminPassword = "Admin123!";

        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16").Build();
        private string _adminToken = string.Empty;

        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor is not null)
                    services.Remove(descriptor);

                services.AddDbContext<AppDbContext>(opt =>
                    opt.UseNpgsql(_postgres.GetConnectionString()));
            });
        }

        public async Task InitializeAsync()
        {
            await _postgres.StartAsync();

            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.EnsureCreatedAsync();

            var saltBytes = RandomNumberGenerator.GetBytes(16);
            var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
                AdminPassword, saltBytes, 310_000, HashAlgorithmName.SHA256, 32);
            var admin = User.Create(
                "Admin",
                AdminLogin,
                Convert.ToBase64String(hashBytes),
                Convert.ToBase64String(saltBytes),
                UserRole.Admin);
            db.Users.Add(admin);
            await db.SaveChangesAsync();

            var loginClient = CreateClient();
            var loginMutation = $$"""
                mutation {
                    users {
                        login(input: { login: "{{AdminLogin}}", password: "{{AdminPassword}}" }) {
                            token
                        }
                    }
                }
                """;
            var loginResponse = await loginClient.PostAsJsonAsync("/graphql", new { query = loginMutation });
            loginResponse.EnsureSuccessStatusCode();
            var loginBody = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
            _adminToken = loginBody
                .GetProperty("data")
                .GetProperty("users")
                .GetProperty("login")
                .GetProperty("token")
                .GetString()!;
        }

        public HttpClient CreateAuthenticatedClient()
        {
            var client = CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _adminToken);
            return client;
        }

        public new async Task DisposeAsync()
        {
            await _postgres.DisposeAsync();
            await base.DisposeAsync();
        }
    }
}
