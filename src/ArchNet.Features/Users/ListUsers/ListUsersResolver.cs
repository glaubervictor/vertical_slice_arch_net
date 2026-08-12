using ArchNet.Features.Users.Shared;
using GraphQL;
using GraphQL.Authorization;
using GraphQL.Types;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace ArchNet.Features.Users.ListUsers;

public sealed class ListUsersResolver : ObjectGraphType
{
    public ListUsersResolver()
    {
        Field<ListUsersResponseType>("users")
            .Argument<NonNullGraphType<IntGraphType>>("page")
            .Argument<NonNullGraphType<IntGraphType>>("pageSize")
            .AuthorizeWithRoles(RoleConstants.Admin, RoleConstants.Manager)
            .ResolveAsync(async ctx =>
            {
                var mediator = ctx.RequestServices!.GetRequiredService<IMediator>();
                var result = await mediator.Send(
                    new ListUsersQuery(
                        ctx.GetArgument<int>("page"),
                        ctx.GetArgument<int>("pageSize")),
                    ctx.CancellationToken);

                if (result.IsSuccess) return result.Value;
                
                ctx.Errors.Add(new ExecutionError(result.Error!.Message) { Code = result.Error.Code });
                return null;
            });
    }
}
