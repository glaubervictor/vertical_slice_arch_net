using ArchNet.Features.Users.Shared;
using GraphQL.Types;

namespace ArchNet.Features.Users.ListUsers;

public sealed class ListUsersResponseType : ObjectGraphType<ListUsersResponse>
{
    public ListUsersResponseType()
    {
        Name = nameof(ListUsersResponseType);
        
        Field(r => r.Total);
        Field<ListGraphType<UserType>>("items")
            .Resolve(ctx => ctx.Source.Items);
    }
}
