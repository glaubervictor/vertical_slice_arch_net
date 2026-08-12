using ArchNet.Api.CurrentUser;
using ArchNet.Common.Interfaces;
using ArchNet.Features.Users.CreateUser;
using ArchNet.Features.Users.LoginUser;

namespace ArchNet.Api.Configurations;

internal static class MediatorConfiguration
{
    internal static void AddMediatorServices(this IServiceCollection services)
    {
        services.AddMediator(opt => opt.ServiceLifetime = ServiceLifetime.Scoped);

        services.AddScoped<CreateUserValidator>();
        services.AddScoped<LoginUserValidator>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUserService>();
    }
}
