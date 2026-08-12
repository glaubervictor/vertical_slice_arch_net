namespace ArchNet.Web.Auth;

public interface ITokenService
{
    ValueTask<string?> GetTokenAsync();
    ValueTask SetTokenAsync(string token);
    ValueTask ClearTokenAsync();
}
