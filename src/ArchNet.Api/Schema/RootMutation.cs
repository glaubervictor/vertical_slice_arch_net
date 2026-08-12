using ArchNet.Features.Users.Shared;
using GraphQL.Types;

namespace ArchNet.Api.Schema;

public sealed class RootMutation : ObjectGraphType
{
    public RootMutation()
    {
        Field<UsersMutation>("users").Resolve(_ => new { });
    }
}
