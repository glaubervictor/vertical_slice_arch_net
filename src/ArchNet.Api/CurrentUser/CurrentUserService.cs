using System.Security.Claims;
using ArchNet.Common.Interfaces;

namespace ArchNet.Api.CurrentUser;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public string Id   => User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    public string Name => User?.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
    public string Role => User?.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
}
