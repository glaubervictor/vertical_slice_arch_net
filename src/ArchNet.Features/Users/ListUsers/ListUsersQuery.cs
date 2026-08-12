using ArchNet.Common.ResultPattern;
using Mediator;

namespace ArchNet.Features.Users.ListUsers;

public record ListUsersQuery(int Page, int PageSize) : IQuery<Result<ListUsersResponse>>;
