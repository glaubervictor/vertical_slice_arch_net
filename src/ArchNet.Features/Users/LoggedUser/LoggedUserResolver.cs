using ArchNet.Features.Users.Shared;
using GraphQL;
using GraphQL.Authorization;
using GraphQL.Types;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace ArchNet.Features.Users.LoggedUser;

public sealed class LoggedUserResolver : ObjectGraphType
{
    public LoggedUserResolver()
    {
        Field<LoggedUserResponseType>("loggedUser")
            .AuthorizeWithRoles(RoleConstants.Admin, RoleConstants.Manager, RoleConstants.User)
            .ResolveAsync(async ctx =>
            {
                var mediator = ctx.RequestServices!.GetRequiredService<IMediator>();
                var result = await mediator.Send(new LoggedUserQuery(), ctx.CancellationToken);

                if (result.IsSuccess) return result.Value;

                ctx.Errors.Add(new ExecutionError(result.Error!.Message) { Code = result.Error.Code });
                return null;
            });
    }
}
