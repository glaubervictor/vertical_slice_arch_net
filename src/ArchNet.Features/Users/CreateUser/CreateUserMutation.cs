using ArchNet.Features.Users.Shared;
using GraphQL;
using GraphQL.Authorization;
using GraphQL.Types;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace ArchNet.Features.Users.CreateUser;

public sealed class CreateUserMutation : ObjectGraphType
{
    public CreateUserMutation()
    {
        Field<UserType>("createUser")
            .Argument<NonNullGraphType<CreateUserInputType>>("input")
            .AuthorizeWithRoles(RoleConstants.Admin)
            .ResolveAsync(async ctx =>
            {
                var mediator = ctx.RequestServices!.GetRequiredService<IMediator>();
                var input = ctx.GetArgument<CreateUserInput>("input");
                var command = new CreateUserCommand(input.Name, input.Login, input.Password, input.Role);
                var result = await mediator.Send(command, ctx.CancellationToken);

                if (result.IsSuccess) return result.Value;

                ctx.Errors.Add(new ExecutionError(result.Error!.Message) { Code = result.Error.Code });
                return null;
            });
    }
}
