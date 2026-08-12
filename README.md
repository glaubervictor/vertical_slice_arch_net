# ArchNet

API GraphQL para gestão de usuários e recursos da plataforma QIATech, construída com **Vertical Slice Architecture** em **.NET 10**.

## Tecnologias

| Área | Biblioteca / Versão |
|---|---|
| Runtime | .NET 10, ASP.NET Core |
| GraphQL | graphql-dotnet `8.8.4` + GraphiQL |
| CQRS / Mediator | Mediator (source-generated) `3.0.2` |
| Validação | FluentValidation `11.12.0` |
| ORM | Entity Framework Core `10.0.5` + Npgsql `10.0.1` |
| Banco de dados | PostgreSQL |
| IDs | CUID2 via `cuid.net` `7.0.0` |
| Autenticação | JWT Bearer `10.0.0` |
| Testes | xUnit + FluentAssertions + Testcontainers |

## Estrutura do Projeto

```
arch_net/
├── src/
│   ├── ArchNet.Api/            # Host ASP.NET Core — AppSchema, RootQuery/Mutation, middleware, Program.cs
│   ├── ArchNet.Features/       # Todos os slices verticais (área principal de desenvolvimento)
│   │   └── Users/
│   │       ├── CreateUser/
│   │       ├── GetUser/
│   │       ├── ListUsers/
│   │       ├── UpdateUser/
│   │       ├── DeleteUser/
│   │       └── Shared/         # ObjectGraphType compartilhado dentro do feature Users
│   ├── ArchNet.Domain/         # Entidades e enums compartilhados entre features
│   ├── ArchNet.Common/         # Result pattern, EntityBase e utilitários cross-cutting
│   └── ArchNet.Infrastructure/ # EF Core DbContext, configurações de entidades, migrations
└── tests/
    ├── UnitTests/              # Testes de domínio e handlers (espelha src/Features/)
    └── IntegrationTests/       # Testes de schema GraphQL e banco de dados
```

### Estrutura de um slice

Cada operação vive em sua própria pasta autocontida:

```
src/ArchNet.Features/Users/CreateUser/
├── CreateUserCommand.cs
├── CreateUserCommandHandler.cs
├── CreateUserValidator.cs
├── CreateUserInputType.cs
└── CreateUserMutation.cs
```

## Decisões de Arquitetura

- **Vertical Slice Architecture** — o código é organizado por feature/caso de uso, não por camada técnica. Slices são independentes e não se importam entre si.
- **Sem Repository Pattern** — handlers injetam `AppDbContext` diretamente.
- **Sem AutoMapper** — mapeamentos são explícitos dentro de cada handler.
- **Result Pattern** — erros de negócio são retornados como `Result<T>`; exceções nunca são usadas para controle de fluxo.
- **IDs com CUID2** — todos os PKs/FKs de entidades usam CUID2 (`string`, `varchar(24)` no PostgreSQL), gerados na camada de aplicação, nunca pelo banco.
- **Gerenciamento Central de Pacotes** — todas as versões NuGet são declaradas em `Directory.Packages.props`; os arquivos `.csproj` referenciam pacotes sem o atributo `Version`.

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL (ou Docker para executá-lo)
- Ferramenta CLI `dotnet-ef`:

```bash
dotnet tool install --global dotnet-ef
```

## Primeiros Passos

1. **Clone o repositório**

```bash
git clone <repo-url>
cd arch_net
```

2. **Configure a connection string**

Defina a variável de ambiente `ConnectionStrings__DefaultConnection` ou atualize o `appsettings.json` em `src/ArchNet.Api`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=archnet;Username=postgres;Password=postgres"
  }
}
```

3. **Aplique as migrations**

```bash
dotnet ef database update -p src/ArchNet.Infrastructure -s src/ArchNet.Api
```

4. **Execute a API**

```bash
dotnet run --project src/ArchNet.Api
```

O playground GraphiQL estará disponível em `http://localhost:<porta>/ui/graphiql`.

## Comandos Úteis

| Tarefa | Comando |
|---|---|
| Build | `dotnet build` |
| Executar testes | `dotnet test` |
| Executar a API | `dotnet run --project src/ArchNet.Api` |
| Formatar código | `dotnet format` |
| Adicionar migration | `dotnet ef migrations add <Nome> -p src/ArchNet.Infrastructure -s src/ArchNet.Api` |
| Atualizar banco | `dotnet ef database update -p src/ArchNet.Infrastructure -s src/ArchNet.Api` |

## Testes

- **Testes unitários** cobrem a lógica de domínio e os handlers CQRS de forma isolada.
- **Testes de integração** sobem uma instância real do PostgreSQL via **Testcontainers** e exercitam o schema GraphQL completo.

```bash
dotnet test                        # todos os testes
dotnet test tests/UnitTests        # somente testes unitários
dotnet test tests/IntegrationTests # somente testes de integração
```
