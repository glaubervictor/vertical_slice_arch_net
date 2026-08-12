using ArchNet.Features.Users.Shared;
using GraphQL;
using GraphQL.Types;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace ArchNet.Features.Users.ChangePassword;

public sealed class ChangePasswordMutation : ObjectGraphType
{
    public ChangePasswordMutation()
    {
        Field<UserType>("changePassword")
            .Argument<NonNullGraphType<ChangePasswordInputType>>("input")
            .AuthorizeWithRoles(RoleConstants.Admin, RoleConstants.Manager, RoleConstants.User)
            .ResolveAsync(async ctx =>
            {
                var mediator = ctx.RequestServices!.GetRequiredService<IMediator>();
                var input = ctx.GetArgument<ChangePasswordInput>("input");
                var command = new ChangePasswordCommand(input.UserId, input.CurrentPassword, input.NewPassword);
                var result = await mediator.Send(command, ctx.CancellationToken);

                if (result.IsSuccess) return result.Value;
                
                ctx.Errors.Add(new ExecutionError(result.Error!.Message) { Code = result.Error.Code });
                return null;
            });
    }
}
