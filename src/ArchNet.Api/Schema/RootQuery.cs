using ArchNet.Features.Users;
using ArchNet.Features.Users.Shared;
using GraphQL.Types;

namespace ArchNet.Api.Schema;

public sealed class RootQuery : ObjectGraphType
{
    public RootQuery()
    {
        Field<UsersQuery>("users").Resolve(_ => new { });
    }
}
