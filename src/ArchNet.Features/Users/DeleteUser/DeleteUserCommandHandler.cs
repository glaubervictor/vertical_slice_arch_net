using ArchNet.Common.ResultPattern;
using ArchNet.Infrastructure.Persistence;
using Mediator;

namespace ArchNet.Features.Users.DeleteUser;

public sealed class DeleteUserCommandHandler(AppDbContext db)
    : ICommandHandler<DeleteUserCommand, Result<DeleteUserResponse>>
{
    public async ValueTask<Result<DeleteUserResponse>> Handle(
        DeleteUserCommand command, CancellationToken cancellationToken)
    {
        var user = await db.Users.FindAsync([command.Id], cancellationToken);

        if (user is null)
            return Result<DeleteUserResponse>.Failure(
                new Error("User.NotFound", $"User '{command.Id}' not found."));

        db.Users.Remove(user);
        await db.SaveChangesAsync(cancellationToken);

        return Result<DeleteUserResponse>.Success(new DeleteUserResponse(command.Id));
    }
}
