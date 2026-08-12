# ArchNet

GraphQL API for managing users and platform resources, built with **Vertical Slice Architecture** on **.NET 10**.

## Tech Stack

| Area | Library / Version |
|---|---|
| Runtime | .NET 10, ASP.NET Core |
| GraphQL | graphql-dotnet `8.8.4` + GraphiQL |
| CQRS / Mediator | Mediator (source-generated) `3.0.2` |
| Validation | FluentValidation `11.12.0` |
| ORM | Entity Framework Core `10.0.5` + Npgsql `10.0.1` |
| Database | PostgreSQL |
| IDs | CUID2 via `cuid.net` `7.0.0` |
| Authentication | JWT Bearer `10.0.0` |
| Testing | xUnit + FluentAssertions + Testcontainers |

## Project Structure

```
arch_net/
├── src/
│   ├── ArchNet.Api/            # ASP.NET Core host — AppSchema, RootQuery/Mutation, middleware, Program.cs
│   ├── ArchNet.Features/       # All vertical slices (main development area)
│   │   └── Users/
│   │       ├── CreateUser/
│   │       ├── GetUser/
│   │       ├── ListUsers/
│   │       ├── UpdateUser/
│   │       ├── DeleteUser/
│   │       └── Shared/         # ObjectGraphType shared within the Users feature
│   ├── ArchNet.Domain/         # Entities and enums shared across features
│   ├── ArchNet.Common/         # Result pattern, EntityBase and cross-cutting utilities
│   └── ArchNet.Infrastructure/ # EF Core DbContext, entity configurations, migrations
└── tests/
    ├── UnitTests/              # Domain and handler tests (mirrors src/Features/)
    └── IntegrationTests/       # GraphQL schema and database tests
```

### Slice structure

Each operation lives in its own self-contained folder:

```
src/ArchNet.Features/Users/CreateUser/
├── CreateUserCommand.cs
├── CreateUserCommandHandler.cs
├── CreateUserValidator.cs
├── CreateUserInputType.cs
└── CreateUserMutation.cs
```

## Architecture Decisions

- **Vertical Slice Architecture** — code is organized by feature/use case, not by technical layer. Slices are independent and never import each other.
- **No Repository Pattern** — handlers inject `AppDbContext` directly.
- **No AutoMapper** — mappings are explicit inside each handler.
- **Result Pattern** — business errors are returned as `Result<T>`; exceptions are never used for flow control.
- **CUID2 IDs** — all entity PKs/FKs use CUID2 (`string`, `varchar(24)` in PostgreSQL), generated in the application layer, never by the database.
- **Central Package Management** — all NuGet versions are declared in `Directory.Packages.props`; `.csproj` files reference packages without the `Version` attribute.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL (or Docker to run it)
- `dotnet-ef` CLI tool:

```bash
dotnet tool install --global dotnet-ef
```

## Getting Started

1. **Clone the repository**

```bash
git clone <repo-url>
cd arch_net
```

2. **Configure the connection string**

Set the `ConnectionStrings__DefaultConnection` environment variable or update `appsettings.json` in `src/ArchNet.Api`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=archnet;Username=postgres;Password=postgres"
  }
}
```

3. **Apply the migrations**

```bash
dotnet ef database update -p src/ArchNet.Infrastructure -s src/ArchNet.Api
```

4. **Run the API**

```bash
dotnet run --project src/ArchNet.Api
```

The GraphiQL playground will be available at `http://localhost:<port>/ui/graphiql`.

## Useful Commands

| Task | Command |
|---|---|
| Build | `dotnet build` |
| Run tests | `dotnet test` |
| Run the API | `dotnet run --project src/ArchNet.Api` |
| Format code | `dotnet format` |
| Add migration | `dotnet ef migrations add <Name> -p src/ArchNet.Infrastructure -s src/ArchNet.Api` |
| Update database | `dotnet ef database update -p src/ArchNet.Infrastructure -s src/ArchNet.Api` |

## Testing

- **Unit tests** cover domain logic and CQRS handlers in isolation.
- **Integration tests** spin up a real PostgreSQL instance via **Testcontainers** and exercise the full GraphQL schema.

```bash
dotnet test                        # all tests
dotnet test tests/UnitTests        # unit tests only
dotnet test tests/IntegrationTests # integration tests only
```
