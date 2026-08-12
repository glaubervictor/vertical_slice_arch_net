using ArchNet.Api.Schema;
using ArchNet.Features.Users.Shared;
using GraphQL;
using GraphQL.Server.Ui.GraphiQL;

namespace ArchNet.Api.Configurations;

internal static class GraphQLConfiguration
{
    internal static void AddGraphQLSchema(this IServiceCollection services)
    {
        services.AddGraphQL(b => b
            .AddSchema<AppSchema>()
            .AddSystemTextJson()
            .AddAuthorizationRule()
            .AddGraphTypes(typeof(AppSchema).Assembly)
            .AddGraphTypes(typeof(UsersQuery).Assembly));
    }

    internal static void UseGraphQLSchema(
        this WebApplication app,
        IWebHostEnvironment environment)
    {
        app.UseGraphQL<AppSchema>();

        if (environment.IsDevelopment())
            app.UseGraphQLGraphiQL(options: new GraphiQLOptions { GraphQLEndPoint = "/graphql" });
    }
}
