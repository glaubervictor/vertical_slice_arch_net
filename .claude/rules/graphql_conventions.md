# GraphQL Conventions

## Stack

- **Pacotes**: `GraphQL` + `GraphQL.Server.Transports.AspNetCore` + `GraphQL.Authorization` (graphql-dotnet)
- **Abordagem**: Code-first com tipos fortemente tipados
- **Endpoint**: `POST /graphql` (+ `GET /graphql` para Playground em dev)

```xml
<PackageReference Include="GraphQL" />
<PackageReference Include="GraphQL.Authorization" />
<PackageReference Include="GraphQL.Server.Transports.AspNetCore" />
```

## Estrutura — Co-localização com Slices

Resolvers GraphQL vivem **dentro do slice**, não em uma pasta centralizada:

```
src/
├── Features/
│   └── Users/
│       ├── LoginUser/
│       │   ├── LoginUserCommand.cs
│       │   ├── LoginUserCommandHandler.cs
│       │   ├── LoginUserValidator.cs
│       │   ├── LoginUserInputType.cs
│       │   ├── LoginUserResponseType.cs
│       │   └── LoginUserMutation.cs     ← público, sem AuthorizeWithRoles
│       ├── CreateUser/
│       │   ├── CreateUserCommand.cs
│       │   ├── CreateUserHandler.cs
│       │   ├── CreateUserValidator.cs
│       │   ├── CreateUserInputType.cs   ← InputObjectGraphType
│       │   └── CreateUserMutation.cs    ← campo da mutation, registrado em RootMutation
│       ├── GetUser/
│       │   ├── GetUserQuery.cs
│       │   ├── GetUserHandler.cs
│       │   ├── GetUserResponse.cs
│       │   └── GetUserResolver.cs       ← campo da query, registrado em RootQuery
│       └── Shared/
│           ├── RoleConstants.cs         ← constantes de roles para AuthorizeWithRoles
│           ├── UserRoleType.cs          ← EnumerationGraphType<UserRole> compartilhado
│           └── UserType.cs              ← ObjectGraphType compartilhado entre slices de User
└── Api/
    ├── Schema/
    │   ├── AppSchema.cs                 ← Schema raiz
    │   ├── RootQuery.cs                 ← Ponto de montagem das queries
    │   └── RootMutation.cs              ← Ponto de montagem das mutations
    └── Program.cs
```

## Schema Raiz

`AppSchema`, `RootQuery` e `RootMutation` são **apenas pontos de montagem** — não contêm lógica:

```csharp
// src/Api/Schema/AppSchema.cs
public class AppSchema : Schema
{
    public AppSchema(IServiceProvider provider) : base(provider)
    {
        Query = provider.GetRequiredService<RootQuery>();
        Mutation = provider.GetRequiredService<RootMutation>();
    }
}
```

## Resolvers por Slice

Cada slice define seu campo GraphQL e o registra via DI:

```csharp
// src/Features/Users/GetUser/GetUserResolver.cs
public class GetUserResolver : ObjectGraphType
{
    public GetUserResolver()
    {
        Field<UserType>("user")
            .Argument<NonNullGraphType<StringGraphType>>("id")
            .AuthorizeWithRoles(RoleConstants.Admin, RoleConstants.Manager)
            .ResolveAsync(async ctx =>
            {
                var mediator = ctx.RequestServices!.GetRequiredService<IMediator>();
                var result = await mediator.Send(
                    new GetUserQuery(ctx.GetArgument<string>("id")),
                    ctx.CancellationToken);

                if (!result.IsSuccess)
                {
                    ctx.Errors.Add(new ExecutionError(result.Error!.Message) { Code = result.Error.Code });
                    return null;
                }
                return result.Value;
            });
    }
}
```

```csharp
// src/Features/Users/CreateUser/CreateUserMutation.cs
public class CreateUserMutation : ObjectGraphType
{
    public CreateUserMutation()
    {
        Field<UserType>("createUser")
            .Argument<NonNullGraphType<CreateUserInputType>>("input")
            .AuthorizeWithRoles(RoleConstants.Admin)
            .ResolveAsync(async ctx =>
            {
                var mediator = ctx.RequestServices!.GetRequiredService<IMediator>();
                var input = ctx.GetArgument<CreateUserInput>("input");
                var command = new CreateUserCommand(input.Name, input.Login, input.Password, input.Role);
                var result = await mediator.Send(command, ctx.CancellationToken);

                if (!result.IsSuccess)
                {
                    ctx.Errors.Add(new ExecutionError(result.Error!.Message) { Code = result.Error.Code });
                    return null;
                }
                return result.Value;
            });
    }
}
```

## RootQuery e RootMutation

Montam os campos a partir dos resolvers dos slices:

```csharp
// src/Api/Schema/RootQuery.cs
public class RootQuery : ObjectGraphType
{
    public RootQuery(UsersQuery usersQuery)
    {
        Field<UsersQuery>("users").Resolve(_ => new { });
    }
}
```

## Enums GraphQL

Enums C# são expostos no schema GraphQL via `EnumerationGraphType<TEnum>`. O tipo fica em `Shared/` quando é compartilhado entre slices:

```csharp
// src/Features/Users/Shared/UserRoleType.cs
public sealed class UserRoleType : EnumerationGraphType<UserRole> { }
```

`EnumerationGraphType<TEnum>` popula automaticamente os valores a partir do enum C#. Os valores no schema GraphQL correspondem ao `.ToString()` de cada membro.

**Em output types** — usar `Field<NonNullGraphType<UserRoleType>, UserRole>` com `.Resolve()` explícito:

```csharp
Field<NonNullGraphType<UserRoleType>, UserRole>("role")
    .Resolve(ctx => ctx.Source.Role);
```

**Em input types** — usar `Field<NonNullGraphType<UserRoleType>>("role")`. O graphql-dotnet mapeia automaticamente pelo nome da propriedade:

```csharp
Field<NonNullGraphType<UserRoleType>>("role");
```

O POCO correspondente usa o tipo C# do enum diretamente:

```csharp
public record CreateUserInput(string Name, string Login, string Password, UserRole Role);
```

O `GetArgument<CreateUserInput>()` deserializa o valor do enum GraphQL para o `UserRole` C# automaticamente — sem `Enum.TryParse` no resolver.

## Input Types

Input types ficam no mesmo slice da mutation:

```csharp
// src/Features/Users/CreateUser/CreateUserInputType.cs
public class CreateUserInputType : InputObjectGraphType<CreateUserInput>
{
    public CreateUserInputType()
    {
        Field(i => i.Name);
        Field(i => i.Login);
        Field(i => i.Password);
        Field<NonNullGraphType<UserRoleType>>("role");
    }
}

public record CreateUserInput(string Name, string Login, string Password, UserRole Role);
```

## UserType Compartilhado

`UserType` fica em `Shared/` porque múltiplos slices o referenciam:

```csharp
// src/Features/Users/Shared/UserType.cs
public class UserType : ObjectGraphType<GetUserResponse>
{
    public UserType()
    {
        Field(u => u.Id);
        Field(u => u.Name);
        Field(u => u.Login);
        Field<NonNullGraphType<UserRoleType>, UserRole>("role")
            .Resolve(ctx => ctx.Source.Role);
        // NUNCA expor PasswordHash ou Salt
    }
}
```

## Registro em Program.cs

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* validação JWT */ });

builder.Services.AddAuthorization();

builder.Services.AddGraphQL(b => b
    .AddSchema<AppSchema>()
    .AddSystemTextJson()
    .AddAuthorizationRule()   // ativa GraphQL.Authorization
    .AddGraphTypes(typeof(AppSchema).Assembly)
    .AddGraphTypes(typeof(UsersQuery).Assembly));

// Ordem obrigatória do pipeline:
app.UseAuthentication();   // ANTES do UseGraphQL
app.UseAuthorization();    // ANTES do UseGraphQL
app.UseGraphQL<AppSchema>();

if (app.Environment.IsDevelopment())
    app.UseGraphQLGraphiQL(options: new GraphiQLOptions { GraphQLEndPoint = "/graphql" });
```

## Autorização

A autorização é feita diretamente no field via `.AuthorizeWithRoles()` do pacote `GraphQL.Authorization`.

### RoleConstants

As roles são definidas como constantes em `src/Features/[Feature]/Shared/RoleConstants.cs`:

```csharp
public static class RoleConstants
{
    public const string Admin   = "Admin";
    public const string Manager = "Manager";
    public const string User    = "User";
}
```

Os valores devem ser idênticos ao `.ToString()` do enum `UserRole` para que o claim gerado no JWT corresponda exatamente.

### Aplicação nos Fields

`.AuthorizeWithRoles()` é inserido na chain **antes** do `.ResolveAsync()`:

```csharp
Field<UserType>("createUser")
    .Argument<NonNullGraphType<CreateUserInputType>>("input")
    .AuthorizeWithRoles(RoleConstants.Admin)          // roles permitidas
    .ResolveAsync(async ctx => { ... });
```

Múltiplas roles = OR (basta ter uma):

```csharp
.AuthorizeWithRoles(RoleConstants.Admin, RoleConstants.Manager)
```

### Mapeamento de roles por operação

| Operação        | Roles permitidas              |
|-----------------|-------------------------------|
| `login`         | pública (sem autorização)     |
| `user`          | Admin, Manager                |
| `users`         | Admin, Manager                |
| `createUser`    | Admin                         |
| `updateUser`    | Admin, Manager                |
| `deleteUser`    | Admin                         |
| `changePassword`| Admin, Manager, User          |

### JWT — Geração do Token

O claim de role deve usar `ClaimTypes.Role` para que o ASP.NET Core popule `User.IsInRole()` corretamente:

```csharp
var claims = new[]
{
    new Claim(ClaimTypes.NameIdentifier, userId),
    new Claim(ClaimTypes.Name,           name),
    new Claim(ClaimTypes.Role,           role)   // valor = UserRole.ToString()
};
```

O token é gerado no `LoginUserHandler` e validado automaticamente pelo middleware `JwtBearer` antes de chegar ao GraphQL.

## Tratamento de Erros

- Resolvers **nunca lançam exceções** para erros de negócio
- Verificar `result.IsSuccess`; se falso, adicionar ao contexto GraphQL:

```csharp
ctx.Errors.Add(new ExecutionError(result.Error!.Message) { Code = result.Error.Code });
return null;
```

## Naming de Enum Values — graphql-dotnet v8

**graphql-dotnet converte nomes de enum para SCREAMING_SNAKE_CASE por padrão** (seguindo a recomendação da spec GraphQL). Isso significa que sem override, `UserRole.Admin` seria serializado como `"ADMIN"`, quebrando o cliente Strawberry Shake que espera `"Admin"` conforme declarado no `schema.graphql`.

**Regra obrigatória**: Todo `EnumerationGraphType<TEnum>` deve sobrescrever `ChangeEnumCase` para preservar o casing original (PascalCase):

```csharp
public sealed class UserRoleType : EnumerationGraphType<UserRole>
{
    protected override string ChangeEnumCase(string val) => val;
}
```

Isso preserva os nomes exatos dos membros do enum C# (`"Admin"`, `"Manager"`, `"User"`), que devem bater com os valores declarados no `schema.graphql` e esperados pelo cliente gerado pelo Strawberry Shake.

---

## Naming de Output Types — graphql-dotnet v8

**graphql-dotnet v8 remove o sufixo `"Type"` do nome da classe ao derivar o nome GraphQL por padrão.** Sem `Name` explícito:

- `LoginUserResponseType` → schema name: `"LoginUserResponse"` ← **errado**
- `UserType` → schema name: `"User"` ← **errado**

Isso causa `NotSupportedException` no cliente Strawberry Shake, pois o `__typename` retornado pelo servidor não bate com o esperado pelo cliente gerado.

**Regra obrigatória**: Todo `ObjectGraphType<T>` cujo nome de classe termina em `"Type"` deve declarar `Name` explicitamente no construtor. Usar `nameof()` — é refactoring-safe e equivalente ao literal em runtime:

```csharp
public sealed class LoginUserResponseType : ObjectGraphType<LoginUserResponse>
{
    public LoginUserResponseType()
    {
        Name = nameof(LoginUserResponseType); // obrigatório — evita stripping do v8
        Field(r => r.Token);
        // ...
    }
}
```

**Exceções (NÃO setar Name explícito):**
- `EnumerationGraphType<T>` como `UserRoleType` → o schema usa `"UserRole"` (sem sufixo), que é o comportamento correto do stripping
- `InputObjectGraphType<T>` como `LoginUserInputType` → o schema usa `"LoginUserInput"`, também correto

**Tipos que requerem `Name` explícito no projeto:**

| Classe | `Name` |
|--------|--------|
| `LoginUserResponseType` | `nameof(LoginUserResponseType)` |
| `UserType` | `nameof(UserType)` |
| `ListUsersResponseType` | `nameof(ListUsersResponseType)` |
| `LoggedUserResponseType` | `nameof(LoggedUserResponseType)` |
| `DeleteUserResponseType` | `nameof(DeleteUserResponseType)` |

---

## Regras Absolutas

- Sem REST endpoints — toda comunicação via GraphQL
- Sem controllers — resolvers são a camada de entrada
- Resolvers são thin: mapeiam argumentos → chamam Mediator → retornam resultado
- Nenhuma lógica de negócio nos resolvers
- Resolvers co-localizados com o slice, nunca em `Api/Schema/`
- `RootQuery`/`RootMutation` apenas montam campos — zero lógica
- `UseAuthentication()` e `UseAuthorization()` sempre **antes** de `UseGraphQL<>()`
- `.AuthorizeWithRoles()` sempre **antes** de `.ResolveAsync()` na chain do field
- Endpoints públicos (ex: `login`) não recebem `.AuthorizeWithRoles()`
