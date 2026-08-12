using ArchNet.Features.Users.Shared;
using GraphQL;
using GraphQL.Authorization;
using GraphQL.Types;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace ArchNet.Features.Users.UpdateUser;

public sealed class UpdateUserMutation : ObjectGraphType
{
    public UpdateUserMutation()
    {
        Field<UserType>("updateUser")
            .Argument<NonNullGraphType<UpdateUserInputType>>("input")
            .AuthorizeWithRoles(RoleConstants.Admin, RoleConstants.Manager)
            .ResolveAsync(async ctx =>
            {
                var mediator = ctx.RequestServices!.GetRequiredService<IMediator>();
                var input = ctx.GetArgument<UpdateUserInput>("input");
                var command = new UpdateUserCommand(input.Id, input.Name, input.Role);
                var result = await mediator.Send(command, ctx.CancellationToken);

                if (result.IsSuccess) return result.Value;

                ctx.Errors.Add(new ExecutionError(result.Error!.Message) { Code = result.Error.Code });
                return null;
            });
    }
}
