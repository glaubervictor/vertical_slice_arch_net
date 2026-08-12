# Blazor Web Rules — ArchNet.Web

## Visão Geral

`ArchNet.Web` é o frontend Blazor **WebAssembly** (.NET 10) da plataforma QIATech. Consome a API GraphQL via Strawberry Shake v15 (cliente fortemente tipado, gerado em build time). UI construída com Blazor Blueprint (componentes shadcn/ui para Blazor).

## Stack

- **SDK**: `Microsoft.NET.Sdk.BlazorWebAssembly`
- **GraphQL Client**: `StrawberryShake.Blazor` v15
- **UI**: `BlazorBlueprint.Components` + `BlazorBlueprint.Icons.Lucide`
- **Auth**: JWT Bearer via `AuthenticationStateProvider` customizado + localStorage
- **Validação de forms**: `Blazored.FluentValidation` (não usar `AddValidation()` de .NET 10 em WASM)

## Estrutura de Pastas

```
src/ArchNet.Web/
├── Auth/                    ← Infraestrutura de autenticação JWT
│   ├── ITokenService.cs
│   ├── TokenService.cs      ← localStorage via IJSRuntime
│   ├── JwtAuthStateProvider.cs
│   └── AuthTokenHandler.cs  ← DelegatingHandler para Bearer token
├── GraphQL/
│   ├── schema.graphql       ← Schema obtido do servidor (dotnet graphql download)
│   └── Operations/          ← Arquivos .graphql por operação
├── Layout/
│   ├── AuthLayout.razor     ← Páginas públicas (Login)
│   └── MainLayout.razor     ← Páginas autenticadas (sidebar)
├── Pages/
│   ├── LoginPage.razor      ← @page "/login", @layout AuthLayout
│   └── Admin/               ← @attribute [Authorize(Roles = "Admin")]
├── Shared/
│   └── RedirectToLogin.razor
├── wwwroot/
│   ├── index.html
│   └── css/app.css
├── .graphqlrc.json
├── _Imports.razor
├── App.razor
└── Program.cs
```

## Strawberry Shake

### Configuração

- `.graphqlrc.json` fica na raiz de `ArchNet.Web/` (irmão do `.csproj`)
- `schema.graphql` fica em `GraphQL/schema.graphql`
- Operações ficam em `GraphQL/Operations/*.graphql`
- O cliente gerado tem nome `ArchNetClient` (namespace `ArchNet.Web.GraphQL`)
- **Sempre** atualizar o `schema.graphql` ao adicionar campos no servidor:
  ```bash
  cd src/ArchNet.Web && dotnet graphql download http://localhost:5000/graphql -o GraphQL
  ```

### Usando o cliente gerado

Injetar `IArchNetClient` nos componentes:

```razor
@inject IArchNetClient Client

@code {
    private async Task Load()
    {
        var result = await Client.GetUsers.ExecuteAsync();
        if (result.IsErrorResult()) { /* tratar erro */ }
        var users = result.Data?.Users?.Users?.Items;
    }
}
```

### Erros

Sempre verificar `result.IsErrorResult()` e acessar `result.Errors.FirstOrDefault()?.Message`.

## Autenticação

### Fluxo

1. Login via mutation `LoginUser` → recebe JWT
2. Salvar no localStorage via `ITokenService.SetTokenAsync(token)`
3. Notificar `JwtAuthStateProvider.NotifyUserAuthenticated(token)`
4. `AuthTokenHandler` injeta o Bearer em todas as requests do Strawberry Shake

### Parse de JWT

O `JwtAuthStateProvider` faz parse manual do payload Base64 do JWT — **sem dependência externa**. Claims mapeados: `sub` → `ClaimTypes.NameIdentifier`, `unique_name` → `ClaimTypes.Name`, `role` → `ClaimTypes.Role`.

### Logout

```csharp
await TokenService.ClearTokenAsync();
AuthProvider.NotifyUserLoggedOut();
Navigation.NavigateTo("/login");
```

## Autorização em Páginas

```razor
@attribute [Authorize]                    ← qualquer usuário autenticado
@attribute [Authorize(Roles = "Admin")]   ← apenas Admin
@attribute [Authorize(Roles = "Admin,Manager")]  ← Admin ou Manager
```

Páginas públicas usam `@layout Layout.AuthLayout` e **não** têm `[Authorize]`.

## Blazor Blueprint

### Prefixo e Namespace

Todos os componentes usam prefixo `Bb*`. Namespace: `BlazorBlueprint.Components` (já em `_Imports.razor`). Ícones: `<LucideIcon Name="nome-do-icone" Class="size-4" />`.

### Componentes Preferidos por Caso de Uso

| Caso de Uso | Componente |
|---|---|
| Container de formulário | `BbCard` + `BbCardHeader/Content/Footer` |
| Campo de texto/senha | `BbFormFieldInput TValue="string"` |
| Select com opções | `BbSelect` + `BbSelectTrigger/Content/Item` |
| Botão principal | `BbButton` |
| Botão destrutivo | `BbButton Variant="ButtonVariant.Destructive"` |
| Feedback inline | `BbAlert` + `BbAlertTitle/Description` |
| Confirmação destrutiva | `BbAlertDialog` (não fecha com Escape) |
| Tabela de dados | `BbDataTable TData="T"` + `BbDataTableColumn` |
| Layout com sidebar | `BbSidebarProvider` + `BbSidebar` + `BbSidebarInset` |

### Loading State

```razor
<BbButton Loading="_loading" LoadingText="Salvando...">Salvar</BbButton>
```

### Mensagens de Erro/Sucesso

Usar `BbAlert` inline (acima do conteúdo do card) — não usar exceções para fluxo de negócio:

```razor
@if (!string.IsNullOrEmpty(_errorMessage))
{
    <BbAlert Variant="AlertVariant.Destructive" Class="mb-4">
        <LucideIcon Name="circle-alert" Class="size-4" />
        <BbAlertTitle>Erro</BbAlertTitle>
        <BbAlertDescription>@_errorMessage</BbAlertDescription>
    </BbAlert>
}
```

## .NET 10 — Blazor Features Utilizadas

| Feature | Uso neste projeto |
|---|---|
| Service lifetime validation (WASM) | Automático em dev mode |
| `NavigationManager.NotFound()` | `App.razor` — seção `<NotFound>` |
| Source-generated validation | **Não usado** — usar `Blazored.FluentValidation` |
| `[PersistentState]` | **Não aplicável** — WASM puro |
| `BrowserHttpReadStream` | **Não aplicável** — sem download de arquivos |

## Regras Absolutas

- **Nunca** expor o JWT em variáveis de state públicas ou URLs
- **Nunca** usar `localStorage` diretamente em C# — sempre via `ITokenService`
- **Nunca** fazer lógica de negócio nos componentes — apenas chamadas ao cliente GraphQL
- **Sempre** usar `@attribute [Authorize]` em páginas que requerem autenticação
- **Sempre** verificar `result.IsErrorResult()` antes de acessar `result.Data`
- Componentes de página ficam em `Pages/`, layouts em `Layout/`, utilitários em `Shared/`
- Operações GraphQL (`.graphql`) devem refletir exatamente o schema do servidor
