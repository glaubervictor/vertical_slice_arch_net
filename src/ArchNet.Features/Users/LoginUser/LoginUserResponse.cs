using ArchNet.Domain.Enums;

namespace ArchNet.Features.Users.LoginUser;

public record LoginUserResponse(string Token, string UserId, string Name, UserRole Role);
