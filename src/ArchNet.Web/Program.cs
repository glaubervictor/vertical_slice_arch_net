using ArchNet.Web.Auth;
using BlazorBlueprint.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<ArchNet.Web.App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Auth
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<JwtAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(
    sp => sp.GetRequiredService<JwtAuthStateProvider>());
builder.Services.AddAuthorizationCore();
builder.Services.AddTransient<AuthTokenHandler>();

// Blazor Blueprint
builder.Services.AddBlazorBlueprintComponents();

// GraphQL — Strawberry Shake
builder.Services
    .AddArchNetClient()
    .ConfigureHttpClient(
        client => client.BaseAddress = new Uri("http://localhost:5000/graphql"),
        clientBuilder => clientBuilder.AddHttpMessageHandler<AuthTokenHandler>());

await builder.Build().RunAsync();
