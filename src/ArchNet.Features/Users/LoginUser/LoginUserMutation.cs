using GraphQL;
using GraphQL.Types;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace ArchNet.Features.Users.LoginUser;

public sealed class LoginUserMutation : ObjectGraphType
{
    public LoginUserMutation()
    {
        Field<LoginUserResponseType>("login")
            .Argument<NonNullGraphType<LoginUserInputType>>("input")
            .ResolveAsync(async ctx =>
            {
                var mediator = ctx.RequestServices!.GetRequiredService<IMediator>();
                var input    = ctx.GetArgument<LoginUserInput>("input");
                var result   = await mediator.Send(
                    new LoginUserCommand(input.Login, input.Password),
                    ctx.CancellationToken);

                if (result.IsSuccess) return result.Value;

                ctx.Errors.Add(new ExecutionError(result.Error!.Message)
                    { Code = result.Error.Code });
                return null;
            });
    }
}
