---
paths:
  - "src/Domain/Entities/User.cs"
  - "src/Infrastructure/Persistence/Configurations/UserConfiguration.cs"
  - "src/Features/Users/**/*"
---

# Users — Domain & Conventions

## Entidade

```csharp
// src/Domain/Entities/User.cs
namespace ArchNet.Domain.Entities;

public class User : EntityBase
{
    public string Name { get; private set; }
    public string Login { get; private set; }
    public string PasswordHash { get; private set; }
    public string Salt { get; private set; }
    public UserRole Role { get; private set; }

    private User() { } // EF Core

    public static User Create(string name, string login, string passwordHash, string salt, UserRole role)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(login);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(salt);

        return new User
        {
            Name = name,
            Login = login,
            PasswordHash = passwordHash,
            Salt = salt,
            Role = role
        };
    }
}
```

## Enum de Role

```csharp
// src/Domain/Enums/UserRole.cs
namespace ArchNet.Domain.Enums;

public enum UserRole { Admin, Manager, User }
```

## Colunas no Banco

| Coluna | Tipo PostgreSQL | Tipo C# | Regras |
|--------|----------------|---------|--------|
| `id` | `varchar(24)` | `string` | PK, CUID2, gerado na app |
| `name` | `text` | `string` | NOT NULL, max 100 |
| `login` | `text` | `string` | NOT NULL, max 100, UNIQUE |
| `password_hash` | `text` | `string` | NOT NULL, max 256 |
| `salt` | `text` | `string` | NOT NULL, max 128 |
| `role` | `text` | `UserRole` | NOT NULL, stored as string |

## Entity Configuration

```csharp
// src/Infrastructure/Persistence/Configurations/UserConfiguration.cs
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id").HasColumnType("varchar(24)").HasMaxLength(24).ValueGeneratedNever();
        builder.Property(u => u.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(u => u.Login).HasColumnName("login").HasMaxLength(100).IsRequired();
        builder.HasIndex(u => u.Login).IsUnique();
        builder.Property(u => u.PasswordHash).HasColumnName("password_hash").HasMaxLength(256).IsRequired();
        builder.Property(u => u.Salt).HasColumnName("salt").HasMaxLength(128).IsRequired();
        builder.Property(u => u.Role)
               .HasColumnName("role")
               .HasConversion<string>()
               .HasMaxLength(50)
               .IsRequired();
    }
}
```

## Estrutura de Slices

```
src/Features/Users/
├── CreateUser/
│   ├── CreateUserCommand.cs         ← record CreateUserCommand(string Name, string Login, string Password, UserRole Role)
│   ├── CreateUserCommandHandler.cs  ← hash da senha aqui, chama User.Create(), persiste
│   ├── CreateUserValidator.cs       ← FluentValidation
│   ├── CreateUserInputType.cs       ← InputObjectGraphType
│   └── CreateUserMutation.cs        ← GraphQL resolver (requer role Admin)
├── GetUser/
│   ├── GetUserQuery.cs              ← record GetUserQuery(string Id)
│   ├── GetUserQueryHandler.cs
│   ├── GetUserResponse.cs           ← record GetUserResponse(string Id, string Name, string Login, UserRole Role)
│   └── GetUserResolver.cs           ← GraphQL resolver
├── ListUsers/
│   ├── ListUsersQuery.cs            ← record ListUsersQuery(int Page, int PageSize)
│   ├── ListUsersQueryHandler.cs
│   ├── ListUsersResponse.cs         ← record ListUsersResponse(IReadOnlyList<GetUserResponse> Items, int Total)
│   ├── ListUsersResponseType.cs     ← ObjectGraphType<ListUsersResponse> (Name = nameof obrigatório)
│   └── ListUsersResolver.cs
├── UpdateUser/
│   ├── UpdateUserCommand.cs         ← record UpdateUserCommand(string Id, string Name, UserRole Role)
│   ├── UpdateUserCommandHandler.cs
│   ├── UpdateUserInputType.cs
│   └── UpdateUserMutation.cs
├── DeleteUser/
│   ├── DeleteUserCommand.cs         ← record DeleteUserCommand(string Id)
│   ├── DeleteUserCommandHandler.cs
│   └── DeleteUserMutation.cs        ← contém DeleteUserResponseType inline (Name = nameof obrigatório)
├── ChangePassword/                  ← operação separada de UpdateUser
│   ├── ChangePasswordCommand.cs
│   ├── ChangePasswordCommandHandler.cs
│   ├── ChangePasswordInputType.cs
│   └── ChangePasswordMutation.cs
├── LoginUser/
│   ├── LoginUserCommand.cs
│   ├── LoginUserCommandHandler.cs   ← valida credenciais, gera JWT
│   ├── LoginUserValidator.cs
│   ├── LoginUserInputType.cs
│   ├── LoginUserResponse.cs
│   ├── LoginUserResponseType.cs     ← ObjectGraphType (Name = nameof obrigatório)
│   └── LoginUserMutation.cs         ← público, sem AuthorizeWithRoles
├── LoggedUser/                      ← retorna o usuário atualmente autenticado
│   ├── LoggedUserQuery.cs
│   ├── LoggedUserQueryHandler.cs
│   ├── LoggedUserResponse.cs        ← record LoggedUserResponse(string Id, string Name, UserRole Role)
│   ├── LoggedUserResponseType.cs    ← ObjectGraphType (Name = nameof obrigatório)
│   └── LoggedUserResolver.cs
└── Shared/
    ├── UserType.cs                  ← ObjectGraphType<GetUserResponse> compartilhado (Name = nameof obrigatório)
    ├── UsersMutation.cs             ← agrega todos os ObjectGraphType de mutation sob o nó "users"
    ├── UsersQuery.cs                ← agrega todos os resolver de query sob o nó "users"
    ├── UserRoleType.cs              ← EnumerationGraphType<UserRole> (ChangeEnumCase para preservar PascalCase)
    └── RoleConstants.cs             ← constantes Admin, Manager, User
```

## Segurança de Senha

- Hash e Salt gerados no `CreateUserHandler` e `ChangePasswordHandler`
- Usar `PBKDF2` via `Rfc2898DeriveBytes` ou `BCrypt.Net`
- **Nunca** expor `PasswordHash` ou `Salt` em nenhum response DTO ou GraphQL type
- Alteração de senha é operação separada (`ChangePasswordCommand`) — nunca dentro de `UpdateUserCommand`

## Regras Absolutas

- `PasswordHash` e `Salt` nunca aparecem em nenhum response DTO ou `UserType`
- Login deve ser único — verificar antes de persistir, retornar `Result.Failure` em conflito
- ID gerado por CUID2 em `EntityBase`, atribuído na factory `User.Create()`
- Cada slice tem seu próprio DTO de response — não existe `UserDto` global
