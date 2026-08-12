using ArchNet.Common.ResultPattern;
using ArchNet.Features.Users.GetUser;
using ArchNet.Infrastructure.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ArchNet.Features.Users.ListUsers;

public sealed class ListUsersQueryHandler(AppDbContext db)
    : IQueryHandler<ListUsersQuery, Result<ListUsersResponse>>
{
    public async ValueTask<Result<ListUsersResponse>> Handle(
        ListUsersQuery query, CancellationToken cancellationToken)
    {
        var skip = (query.Page - 1) * query.PageSize;

        var total = await db.Users.AsNoTracking().CountAsync(cancellationToken);
        var items = await db.Users
            .AsNoTracking()
            .OrderBy(u => u.Name)
            .Skip(skip)
            .Take(query.PageSize)
            .Select(u => new GetUserResponse(u.Id, u.Name, u.Login, u.Role))
            .ToListAsync(cancellationToken);

        return Result<ListUsersResponse>.Success(new ListUsersResponse(items, total));
    }
}
