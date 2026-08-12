---
paths:
  - "tests/**/*"
---

# Testing Rules

## Stack

```xml
<PackageReference Include="xunit" />
<PackageReference Include="xunit.runner.visualstudio" />
<PackageReference Include="Microsoft.NET.Test.Sdk" />
<PackageReference Include="FluentAssertions" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
<PackageReference Include="Testcontainers.PostgreSql" />
```

## Estrutura — Espelhando Slices

Os testes espelham a estrutura de `src/Features/`:

```
tests/
├── UnitTests/
│   ├── Domain/                          ← Testes de entidades (User.Create, etc.)
│   └── Features/
│       └── Users/
│           ├── CreateUserCommandHandlerTests.cs
│           ├── GetUserQueryHandlerTests.cs
│           ├── ListUsersQueryHandlerTests.cs
│           ├── UpdateUserCommandHandlerTests.cs
│           ├── DeleteUserHandlerTests.cs
│           └── ChangePasswordCommandHandlerTests.cs
└── IntegrationTests/
    ├── Features/
    │   └── Users/
    │       └── UserGraphQLTests.cs      ← Testes do schema GraphQL via HTTP
    └── Persistence/
        └── UserConfigurationTests.cs    ← Testes de EF Core configuration
```

## Nomeação

```
[Método]_[Cenário]_[ResultadoEsperado]

Exemplos:
User_Create_WithValidData_ReturnsUser
User_Create_WithEmptyName_ThrowsArgumentException
Handle_WithValidCommand_CreatesUserAndReturnsResponse
Handle_WithDuplicateLogin_ReturnsFailure
Handle_WhenUserNotFound_ReturnsFailure
```

## Unit Tests — Domain

Testam a lógica das entidades sem dependências externas:

```csharp
public class UserTests
{
    [Fact]
    public void User_Create_WithValidData_ReturnsUser()
    {
        var user = User.Create("Alice", "alice@qia.tech", "hash", "salt", UserRole.User);

        user.Name.Should().Be("Alice");
        user.Login.Should().Be("alice@qia.tech");
        user.Role.Should().Be(UserRole.User);
        user.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void User_Create_WithEmptyName_ThrowsArgumentException()
    {
        var act = () => User.Create("", "alice@qia.tech", "hash", "salt", UserRole.User);

        act.Should().Throw<ArgumentException>();
    }
}
```

## Unit Tests — Handlers

Testam handlers com banco real via Testcontainers. Instanciar o handler diretamente com todas as dependências, incluindo validators:

```csharp
public class CreateUserCommandHandlerTests : IAsyncLifetime
{
    private AppDbContext _db = null!;
    private PostgreSqlContainer _postgres = null!;

    public async Task InitializeAsync()
    {
        _postgres = new PostgreSqlBuilder().Build();
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

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
```

### Helpers de teste

Quando o teste precisar de dados com lógica de domínio (ex: hash de senha para seed), criar métodos privados estáticos no próprio arquivo de teste:

```csharp
private static (string hash, string salt) HashPassword(string password)
{
    var saltBytes = RandomNumberGenerator.GetBytes(16);
    var hash = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, 310_000, HashAlgorithmName.SHA256, 32);
    return (Convert.ToBase64String(hash), Convert.ToBase64String(saltBytes));
}
```

## Integration Tests — GraphQL

Testam o schema completo via HTTP com `WebApplicationFactory`. A fixture:

- Substitui o `DbContext` pelo Testcontainers PostgreSQL
- Semeia um admin diretamente no DB (sem passar pelo GraphQL)
- Faz login via mutation GraphQL para obter um JWT
- Expõe `CreateAuthenticatedClient()` para testes que requerem auth

### Estrutura do schema GraphQL (nested)

O schema usa agrupamento por feature — todas as operações de usuário ficam sob o nó `users`:

```graphql
mutation {
    users {
        createUser(input: { name: "Alice", login: "alice@qia.tech", password: "Secret123!", role: User }) {
            id name login role
        }
    }
}

query {
    users {
        user(id: "abc123") { id name login role }
        users(page: 1, pageSize: 10) { total items { id name login role } }
    }
}
```

> **Atenção**: Valores de enum GraphQL **não** levam aspas — `role: User`, não `role: "User"`.

### Padrão ApiFixture

```csharp
public class UserGraphQLTests : IClassFixture<UserGraphQLTests.ApiFixture>
{
    private readonly ApiFixture _fixture;

    public UserGraphQLTests(ApiFixture fixture) => _fixture = fixture;

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
        body.GetProperty("data").GetProperty("users").GetProperty("users")
            .GetProperty("total").GetInt32().Should().BeGreaterThanOrEqualTo(0);
    }

    public class ApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private const string AdminLogin    = "admin@integration.test";
        private const string AdminPassword = "Admin123!";

        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16").Build();
        private string _adminToken = string.Empty;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
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

            // Seed admin diretamente no DB (sem passar pelo GraphQL)
            var saltBytes = RandomNumberGenerator.GetBytes(16);
            var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
                AdminPassword, saltBytes, 310_000, HashAlgorithmName.SHA256, 32);
            var admin = User.Create(
                "Admin", AdminLogin,
                Convert.ToBase64String(hashBytes),
                Convert.ToBase64String(saltBytes),
                UserRole.Admin);
            db.Users.Add(admin);
            await db.SaveChangesAsync();

            // Login via GraphQL para obter JWT
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
                .GetProperty("data").GetProperty("users").GetProperty("login")
                .GetProperty("token").GetString()!;
        }

        /// <summary>Retorna HttpClient com Bearer token do admin já configurado.</summary>
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
```

### Fixture scoping

| Tipo de teste | Padrão | Isolamento de banco |
|---|---|---|
| Unit (handler) | `IAsyncLifetime` por classe | Container novo por classe — DB isolado |
| Integration (GraphQL) | `IClassFixture<ApiFixture>` | Container compartilhado entre tests da mesma classe |

## Regras Absolutas

- **Sem mocks de DbContext** — usar banco real via Testcontainers
- **Sem mocks de Mediator** — testar handlers diretamente ou via pipeline completo
- Cada teste é independente — banco limpo por fixture/coleção
- `FluentAssertions` obrigatório — sem `Assert.Equal` puro
- `CancellationToken.None` em unit tests; token real em integration tests
- Integration tests usam PostgreSQL via Testcontainers (nunca SQLite)
- Estrutura de pastas de testes espelha `src/Features/`
- Cada teste cria os dados que precisa — sem fixtures globais compartilhadas
