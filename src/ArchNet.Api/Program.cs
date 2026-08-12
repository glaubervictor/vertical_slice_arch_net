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

namespace ArchNet.Api
{
    public partial class Program { }
}
