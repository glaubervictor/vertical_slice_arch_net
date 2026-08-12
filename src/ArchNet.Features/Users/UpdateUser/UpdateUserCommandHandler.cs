using ArchNet.Common.ResultPattern;
using ArchNet.Features.Users.GetUser;
using ArchNet.Infrastructure.Persistence;
using Mediator;

namespace ArchNet.Features.Users.UpdateUser;

public sealed class UpdateUserCommandHandler(AppDbContext db)
    : ICommandHandler<UpdateUserCommand, Result<GetUserResponse>>
{
    public async ValueTask<Result<GetUserResponse>> Handle(
        UpdateUserCommand command, CancellationToken cancellationToken)
    {
        var user = await db.Users.FindAsync([command.Id], cancellationToken);

        if (user is null)
            return Result<GetUserResponse>.Failure(
                new Error("User.NotFound", $"User '{command.Id}' not found."));

        user.UpdateProfile(command.Name, command.Role);
        await db.SaveChangesAsync(cancellationToken);

        return Result<GetUserResponse>.Success(
            new GetUserResponse(user.Id, user.Name, user.Login, user.Role));
    }
}
