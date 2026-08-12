using ArchNet.Domain.Enums;

namespace ArchNet.Features.Users.GetUser;

public record GetUserResponse(string Id, string Name, string Login, UserRole Role);
