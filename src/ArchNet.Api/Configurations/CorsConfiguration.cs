namespace ArchNet.Api.Configurations;

internal static class CorsConfiguration
{
    private const string PolicyName = "AllowAll";

    internal static void AddCorsPolicy(this IServiceCollection services)
    {
        services.AddCors(options =>
            options.AddPolicy(PolicyName, policy => policy
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader()));
    }

    internal static void UseCorsPolicy(this WebApplication app)
    {
        app.UseCors(PolicyName);
    }
}
