using ArchNet.Features.Users.Shared;
using GraphQL;
using GraphQL.Authorization;
using GraphQL.Types;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace ArchNet.Features.Users.GetUser;

public sealed class GetUserResolver : ObjectGraphType
{
    public GetUserResolver()
    {
        Field<UserType>("user")
            .Argument<NonNullGraphType<StringGraphType>>("id")
            .AuthorizeWithRoles(RoleConstants.Admin, RoleConstants.Manager)
            .ResolveAsync(async ctx =>
            {
                var mediator = ctx.RequestServices!.GetRequiredService<IMediator>();
                var result = await mediator.Send(
                    new GetUserQuery(ctx.GetArgument<string>("id")),
                    ctx.CancellationToken);

                if (result.IsSuccess) return result.Value;

                ctx.Errors.Add(new ExecutionError(result.Error!.Message) { Code = result.Error.Code });
                return null;
            });
    }
}
