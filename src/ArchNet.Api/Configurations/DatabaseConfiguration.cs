using ArchNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ArchNet.Api.Configurations;

internal static class DatabaseConfiguration
{
    internal static void AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseNpgsql(configuration.GetConnectionString("Default")));
    }
}
