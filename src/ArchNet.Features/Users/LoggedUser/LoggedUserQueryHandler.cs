using ArchNet.Common.Interfaces;
using ArchNet.Common.ResultPattern;
using ArchNet.Domain.Enums;
using Mediator;

namespace ArchNet.Features.Users.LoggedUser;

public sealed class LoggedUserQueryHandler(ICurrentUser currentUser)
    : IQueryHandler<LoggedUserQuery, Result<LoggedUserResponse>>
{
    public ValueTask<Result<LoggedUserResponse>> Handle(
        LoggedUserQuery query, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
            return ValueTask.FromResult(Result<LoggedUserResponse>.Failure(
                new Error("LoggedUser.Unauthenticated", "No authenticated user in the current context.")));

        var role = Enum.Parse<UserRole>(currentUser.Role);

        return ValueTask.FromResult(Result<LoggedUserResponse>.Success(
            new LoggedUserResponse(currentUser.Id, currentUser.Name, role)));
    }
}
