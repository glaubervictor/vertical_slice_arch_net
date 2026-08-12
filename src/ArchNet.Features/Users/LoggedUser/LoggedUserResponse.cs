using ArchNet.Domain.Enums;

namespace ArchNet.Features.Users.LoggedUser;

public record LoggedUserResponse(string Id, string Name, UserRole Role);
