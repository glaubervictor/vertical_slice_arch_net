using ArchNet.Features.Users.GetUser;

namespace ArchNet.Features.Users.ListUsers;

public record ListUsersResponse(IReadOnlyList<GetUserResponse> Items, int Total);
