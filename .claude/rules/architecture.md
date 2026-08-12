# Architecture Rules — Vertical Slice Architecture

## Princípio

> **"Minimize coupling between slices, maximize coupling within a slice."**

O código é organizado por **features (casos de uso)**, não por camadas técnicas. Cada operação (CreateUser, GetUser, etc.) é um slice autônomo contendo tudo que precisa para funcionar.

## Estrutura de Projeto

```
src/
├── Common/                          ← Cross-cutting sem dependência de feature
│   ├── ResultPattern/
│   │   └── Result.cs
│   └── Primitives/
│       └── EntityBase.cs            ← Base com Id CUID2
├── Domain/                          ← Entidades compartilhadas entre slices
│   ├── Entities/
│   │   └── User.cs
│   └── Enums/
│       └── UserRole.cs
├── Features/                        ← TODOS os slices aqui
│   └── Users/
│       ├── CreateUser/
│       │   ├── CreateUserCommand.cs
│       │   ├── CreateUserCommandHandler.cs
│       │   ├── CreateUserValidator.cs
│       │   ├── CreateUserInputType.cs
│       │   └── CreateUserMutation.cs
│       ├── GetUser/
│       │   ├── GetUserQuery.cs
│       │   ├── GetUserHandler.cs
│       │   ├── GetUserResponse.cs
│       │   └── GetUserResolver.cs
│       ├── ListUsers/
│       │   ├── ListUsersQuery.cs
│       │   ├── ListUsersHandler.cs
│       │   ├── ListUsersResponse.cs
│       │   └── ListUsersResolver.cs
│       ├── UpdateUser/
│       │   └── ...
│       ├── DeleteUser/
│       │   └── ...
│       └── Shared/
│           └── UserType.cs          ← ObjectGraphType compartilhado entre slices de User
├── Infrastructure/                  ← Compartilhado — EF Core, migrations
│   └── Persistence/
│       ├── AppDbContext.cs
│       ├── Configurations/
│       └── Migrations/
└── Api/                             ← Host ASP.NET Core
    ├── Configurations/              ← Extension methods por responsabilidade
    │   ├── AuthenticationConfiguration.cs  ← JWT Bearer + Authorization
    │   ├── CorsConfiguration.cs            ← CORS policy
    │   ├── DatabaseConfiguration.cs        ← EF Core / PostgreSQL
    │   ├── GraphQLConfiguration.cs         ← GraphQL schema + middleware
    │   └── MediatorConfiguration.cs        ← Mediator, validators, CurrentUser
    ├── Schema/
    │   ├── AppSchema.cs
    │   ├── RootQuery.cs             ← Ponto de montagem: registra campos dos slices
    │   └── RootMutation.cs
    ├── CurrentUser/
    └── Program.cs                   ← apenas orquestração
```

## O que vai onde

### Por slice (`src/Features/[Feature]/[SliceName]/`)
- Command ou Query
- Handler
- FluentValidation Validator
- DTO de response (ex: `GetUserResponse.cs`)
- GraphQL resolver (Query resolver ou Mutation)
- GraphQL input type (se mutation)

### Compartilhado dentro de um feature (`src/Features/[Feature]/Shared/`)
- `UserType.cs` — ObjectGraphType usado por múltiplos slices do mesmo feature

### Compartilhado global (`src/Domain/`, `src/Common/`)
- Entidades de domínio (usadas por múltiplos features)
- `EntityBase`, `Result<T>`, `Error` — primitivos de toda a aplicação

### Infraestrutura (`src/Infrastructure/`)
- `AppDbContext` — único, centralizado
- `IEntityTypeConfiguration<T>` por entidade
- Migrations

### Api (`src/Api/`)
- `AppSchema`, `RootQuery`, `RootMutation` — apenas ponto de montagem do schema GraphQL
- `Program.cs` — apenas orquestração: chama os extension methods de `Configurations/`
- `Configurations/` — um arquivo por responsabilidade, cada um expondo extension methods sobre `IServiceCollection` ou `WebApplication`

## Modularização do Program.cs

O `Program.cs` deve conter **apenas orquestração** — chamadas de extension methods, sem lógica de configuração inline.

Toda configuração vive em `src/Api/Configurations/`, um arquivo por responsabilidade:

| Arquivo | Extension methods |
|---|---|
| `AuthenticationConfiguration.cs` | `AddJwtAuthentication(IConfiguration)` |
| `CorsConfiguration.cs` | `AddCorsPolicy()`, `UseCorsPolicy()` |
| `DatabaseConfiguration.cs` | `AddDatabase(IConfiguration)` |
| `GraphQLConfiguration.cs` | `AddGraphQLSchema()`, `UseGraphQLSchema(IWebHostEnvironment)` |
| `MediatorConfiguration.cs` | `AddMediatorServices()` |

**Convenções:**
- Prefixo `Add*` para métodos em `IServiceCollection` (registro de serviços)
- Prefixo `Use*` para métodos em `WebApplication` (pipeline HTTP)
- Classes `internal static` — não fazem parte da API pública do projeto
- File-scoped namespace `ArchNet.Api.Configurations`

**Program.cs resultante:**

```csharp
using ArchNet.Api.Configurations;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddMediatorServices();
builder.Services.AddGraphQLSchema();
builder.Services.AddCorsPolicy();

var app = builder.Build();

app.UseRouting();
app.UseCorsPolicy();
app.UseAuthentication();
app.UseAuthorization();
app.UseGraphQLSchema(app.Environment);

await app.RunAsync();
```

## CORS

A política `"AllowAll"` é encapsulada em `CorsConfiguration.cs` e permite qualquer origem, método e header. O middleware `UseCorsPolicy()` deve ser chamado **antes** de `UseGraphQL` (já garantido por `UseGraphQLSchema`).

## Regra de Dependência entre Slices

**Slices não se importam entre si.** Se dois slices precisam do mesmo código, ele vai para:
- `src/Features/[Feature]/Shared/` — se é específico do feature
- `src/Domain/` — se é uma entidade de domínio
- `src/Common/` — se é utilitário genérico

## Domínios Compartilhados

> **"Minimize coupling between slices, maximize coupling within a slice."**

Quando código precisa ser compartilhado, a decisão é sobre **onde ele mora** — guiada por **quem o consome**. Compartilhar **dado** e **contrato** é barato e correto; compartilhar **comportamento** é caro — só subir de escopo quando for uma invariante de domínio real, nunca por conveniência.

### Três níveis de escopo

**Nível 1 — dentro de um único feature → `Features/[Feature]/Shared/`**

Código usado por múltiplos slices do mesmo feature. É o padrão já em uso (`Users/Shared/` com `UserType`, `UserRoleType`, `RoleConstants`).

**Nível 2 — dado compartilhado entre features → `Domain/` ou `Common/`**

Entidade ou enum consumido por mais de um domínio (ex: `User` referenciado por `Orders`). Caso sem atrito: dado compartilhado sobe de nível, slices continuam sem se importar.

- Entidade / enum de domínio → `src/Domain/Entities`, `src/Domain/Enums`
- Primitivo genérico (`Result`, `EntityBase`, `Error`) → `src/Common`
- `AppDbContext` permanece único em `Infrastructure` — todos os domínios compartilham

**Nível 3 — comportamento cross-domain (o caso difícil)**

Um slice de um domínio precisa de lógica que vive em outro. **Nunca** referenciar o handler/validator do outro slice. Opções, em ordem de preferência:

1. **Query via `IMediator`** — leitura cross-slice. O slice de `Orders` envia `GetUserQuery` pelo Mediator. Acoplamento fica no contrato (Query/Response), não na implementação. **Preferido para leitura.**
2. **`AppDbContext` direto** — `db.Users.FirstOrDefaultAsync(...)`. Permitido (sem Repository). Aceitável para checagem trivial de existência / FK; o custo é o domínio consumidor passar a conhecer o schema do outro.
3. **Serviço de domínio → `src/Domain/Services/`** — apenas quando é invariante de domínio genuína que múltiplos domínios devem aplicar igual (ex: política de senha). Injetável, sem estado de slice. Se só um domínio usa, **não** é compartilhado.
4. **Domain event** — efeito colateral entre domínios sem acoplamento síncrono (ex: `UserDeleted` → `Orders` reage). Só quando o desacoplamento temporal importa; não adotar cedo.

### Tabela de decisão

| O que compartilhar | Onde |
|---|---|
| Dado (entidade / enum) | `Domain/` ou `Common/` |
| Contrato de leitura | Query via `IMediator` |
| Regra de domínio invariante | `Domain/Services/` |
| Efeito desacoplado | Domain event |
| Código de 2 slices do mesmo feature | `Features/[Feature]/Shared/` |

### Anti-padrões

- Slice importar handler / validator de outro slice — acoplamento proibido
- `Features/Shared/` **global** genérico — vira lixeira e mata o benefício de VSA
- DTO de response **global** por entidade — cada slice tem o seu próprio response
- Repository Pattern para "abstrair" acesso cross-domain — proibido no projeto

## Identity — CUID2

Todos os IDs de entidades (PKs e FKs) **obrigatoriamente** usam CUID2.

```csharp
// src/Common/Primitives/EntityBase.cs
public abstract class EntityBase
{
    public string Id { get; private init; } = new Cuid2().ToString();
}
```

- Tipo no banco: `varchar(24)` (PostgreSQL)
- Tipo no C#: `string`
- Nunca usar `int`, `long` ou `Guid` como PK/FK
- Geração sempre na aplicação, nunca no banco

**Pacote NuGet:** `cuid.net` — namespace: `Visus.Cuid`
```xml
<PackageReference Include="cuid.net" />
```

## CQRS com Mediator

- **Commands**: mutam estado, retornam `Result<T>` ou `Result`
- **Queries**: leitura pura, retornam `Result<T>`
- Pipeline behaviors no Mediator para validação e logging (registrados globalmente)

**Naming por slice:**
```
src/Features/Users/CreateUser/
  CreateUserCommand.cs
  CreateUserHandler.cs        ← ICommandHandler<CreateUserCommand, Result<CreateUserResponse>>
  CreateUserValidator.cs
```

## Result Pattern

Nunca lançar exceções para erros de negócio:

```csharp
// src/Common/ResultPattern/Result.cs
public record Result<T>(T? Value, Error? Error)
{
    public bool IsSuccess => Error is null;
    public static Result<T> Success(T value) => new(value, null);
    public static Result<T> Failure(Error error) => new(default, error);
}

public record Error(string Code, string Message);
```

## Gerenciamento Central de Pacotes (CPM)

Todas as versões de pacotes NuGet são centralizadas em `Directory.Packages.props` na raiz do repositório. Os arquivos `.csproj` declaram `<PackageReference>` **sem o atributo `Version`**.

```xml
<!-- Directory.Packages.props (raiz) -->
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="FluentValidation" Version="11.12.0" />
    <!-- demais pacotes... -->
  </ItemGroup>
</Project>
```

```xml
<!-- Em qualquer .csproj -->
<PackageReference Include="FluentValidation" />
```

- Para sobrescrever pontualmente em um projeto: `<PackageReference Include="Pkg" VersionOverride="x.y.z" />`
- Versões flutuantes (`3.*`) são proibidas — usar sempre versão exata
- Ao adicionar um novo pacote, declarar a versão no `Directory.Packages.props` e referenciar sem versão no `.csproj`

## Regras Absolutas

- Sem Repository Pattern — handlers injetam `AppDbContext` diretamente
- Sem AutoMapper — mapeamentos explícitos dentro de cada handler
- Sem Stored Procedures
- Sem exceções para controle de fluxo de negócio
- Construtores primários para DI
- Records para DTOs, Commands e Queries
- File-scoped namespaces em todos os arquivos
- Sempre passar `CancellationToken` para métodos assíncronos
- Cada slice tem seu próprio DTO de response — sem DTO global por entidade
