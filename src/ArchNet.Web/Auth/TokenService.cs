using Microsoft.JSInterop;

namespace ArchNet.Web.Auth;

public sealed class TokenService(IJSRuntime js) : ITokenService
{
    private const string Key = "archnet_token";

    public async ValueTask<string?> GetTokenAsync() =>
        await js.InvokeAsync<string?>("localStorage.getItem", Key);

    public async ValueTask SetTokenAsync(string token) =>
        await js.InvokeVoidAsync("localStorage.setItem", Key, token);

    public async ValueTask ClearTokenAsync() =>
        await js.InvokeVoidAsync("localStorage.removeItem", Key);
}
