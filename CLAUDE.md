# CLAUDE.md - ArchNet

## Overview
API GraphQL para gestão de usuários e recursos da plataforma QIATech, construída com Vertical Slice Architecture em .NET 10.

## Tech Stack
- .NET 10, ASP.NET Core
- Entity Framework Core 10 com PostgreSQL (Npgsql 10.x)
- Mediator para CQRS (source-generated, https://github.com/martinothamar/Mediator)
- FluentValidation para validação de requests
- GraphQL via graphql-dotnet (`GraphQL` + `GraphQL.Server.Transports.AspNetCore`)
- xUnit + FluentAssertions para testes
- CUID2 (pacote `cuid.net`, namespace `Visus.Cuid`) para geração de IDs

## Project Structure
- `src/Features/` - Todos os slices por feature e operação *(ponto central de desenvolvimento)*
- `src/Domain/` - Entidades e enums compartilhados entre features
- `src/Common/` - Result pattern, EntityBase e utilitários cross-cutting
- `src/Infrastructure/` - EF Core DbContext, configurations, migrations
- `src/Api/` - AppSchema, RootQuery/RootMutation (montagem), middleware, Program.cs
- `tests/UnitTests/` - Domain e handlers (espelha `src/Features/`)
- `tests/IntegrationTests/` - Schema GraphQL e banco de dados

## Commands
- Build: `dotnet build`
- Test: `dotnet test`
- Run API: `dotnet run --project src/Api`
- Add Migration: `dotnet ef migrations add <Name> -p src/Infrastructure -s src/Api`
- Update Database: `dotnet ef database update -p src/Infrastructure -s src/Api`
- Update Database on Windows: `dotnet ef database update --project "C:\Projetos\qiatech\arch_net\src\ArchNet.Infrastructure\ArchNet.Infrastructure.csproj" --startup-project "C:\Projetos\qiatech\arch_net\src\ArchNet.Api\ArchNet.Api.csproj"`
- Format: `dotnet format`

## Rules
Regras detalhadas estão em `.claude/rules/`:
- `architecture.md` — VSA, estrutura de slices, CUID2, Result pattern, CQRS naming
- `database.md` — EF Core 10 + Npgsql 10, configurations, convenções de coluna *(carregado ao editar `src/Infrastructure/**`)*
- `graphql_conventions.md` — GraphQL schema, resolvers, mutations, autorização, erros
- `users.md` — entidade User, colunas, segurança de senha, DTOs *(carregado ao editar arquivos de User)*
- `testing.md` — xUnit + FluentAssertions, Testcontainers, naming *(carregado ao editar `tests/**`)*
- `blazor_web.md` — Blazor WebAssembly, Strawberry Shake, Blazor Blueprint, autenticação JWT *(carregado ao editar `src/ArchNet.Web/**`)*
