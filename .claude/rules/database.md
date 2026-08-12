---
paths:
  - "src/Infrastructure/**/*"
---

# Database Rules

## Stack

- **ORM**: Entity Framework Core 10
- **Provider**: Npgsql.EntityFrameworkCore.PostgreSQL 10.x
- **Banco**: PostgreSQL (versão mais recente suportada pelo Npgsql 10.x)

```xml
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
</PackageReference>
```

## DbContext

Um único `AppDbContext` em `src/Infrastructure/Persistence/`:

```csharp
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

## Entity Configurations

Cada entidade tem sua própria `IEntityTypeConfiguration<T>` em `src/Infrastructure/Persistence/Configurations/`:

```csharp
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasMaxLength(24).ValueGeneratedNever();

        builder.Property(u => u.Name).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Login).HasMaxLength(100).IsRequired();
        builder.HasIndex(u => u.Login).IsUnique();

        builder.Property(u => u.PasswordHash).HasMaxLength(256).IsRequired();
        builder.Property(u => u.Salt).HasMaxLength(128).IsRequired();

        builder.Property(u => u.Role)
               .HasConversion<string>()
               .HasMaxLength(50)
               .IsRequired();
    }
}
```

## Convenções de Coluna

| C# Type | PostgreSQL Type |
|---------|----------------|
| `string` (ID/PK/FK) | `varchar(24)` |
| `string` (Name, Login) | `text` com `HasMaxLength` |
| `enum` | `text` via `.HasConversion<string>()` |
| `DateTimeOffset` | `timestamptz` |
| `bool` | `boolean` |

## Migrations

- Sempre criar migration nomeada descritivamente: `dotnet ef migrations add AddUserTable -p src/Infrastructure -s src/Api`
- Nunca editar migrations geradas — criar nova migration para correções
- Migrations ficam em `src/Infrastructure/Persistence/Migrations/`

## Acesso a Dados

- Handlers injetam `AppDbContext` diretamente — sem Repository Pattern
- Usar `AsNoTracking()` em queries de leitura
- Usar `.FindAsync(id, ct)` para busca por PK
- Sempre passar `CancellationToken`

```csharp
// Handler de query — leitura
var user = await _db.Users
    .AsNoTracking()
    .FirstOrDefaultAsync(u => u.Id == query.Id, cancellationToken);

// Handler de command — escrita
_db.Users.Add(user);
await _db.SaveChangesAsync(cancellationToken);
```

## Regras Absolutas

- Sem Stored Procedures
- Sem Raw SQL (exceto migrations de dados em casos extremos, documentado)
- Sem Repository Pattern
- PKs sempre do tipo `varchar(24)` com valor gerado pela aplicação (CUID2)
- FKs seguem o mesmo tipo `varchar(24)` da PK referenciada
