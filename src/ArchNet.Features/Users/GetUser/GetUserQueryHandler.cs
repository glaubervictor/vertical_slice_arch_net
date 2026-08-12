using ArchNet.Common.ResultPattern;
using ArchNet.Infrastructure.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ArchNet.Features.Users.GetUser;

public sealed class GetUserQueryHandler(AppDbContext db)
    : IQueryHandler<GetUserQuery, Result<GetUserResponse>>
{
    public async ValueTask<Result<GetUserResponse>> Handle(
        GetUserQuery query, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == query.Id, cancellationToken);

        if (user is null)
            return Result<GetUserResponse>.Failure(
                new Error("User.NotFound", $"User '{query.Id}' not found."));

        return Result<GetUserResponse>.Success(
            new GetUserResponse(user.Id, user.Name, user.Login, user.Role));
    }
}
