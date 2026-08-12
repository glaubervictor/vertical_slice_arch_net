using ArchNet.Features.Users.Shared;
using GraphQL;
using GraphQL.Authorization;
using GraphQL.Types;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace ArchNet.Features.Users.DeleteUser;

public sealed class DeleteUserResponseType : ObjectGraphType<DeleteUserResponse>
{
    public DeleteUserResponseType()
    {
        Name = nameof(DeleteUserResponseType);
        Field(r => r.Id);
    }
}

public sealed class DeleteUserMutation : ObjectGraphType
{
    public DeleteUserMutation()
    {
        Field<DeleteUserResponseType>("deleteUser")
            .Argument<NonNullGraphType<StringGraphType>>("id")
            .AuthorizeWithRoles(RoleConstants.Admin)
            .ResolveAsync(async ctx =>
            {
                var mediator = ctx.RequestServices!.GetRequiredService<IMediator>();
                var result = await mediator.Send(
                    new DeleteUserCommand(ctx.GetArgument<string>("id")),
                    ctx.CancellationToken);

                if (result.IsSuccess) return result.Value;
                
                ctx.Errors.Add(new ExecutionError(result.Error!.Message) { Code = result.Error.Code });
                return null;
            });
    }
}
