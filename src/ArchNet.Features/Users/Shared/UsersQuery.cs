using ArchNet.Features.Users.GetUser;
using ArchNet.Features.Users.ListUsers;
using ArchNet.Features.Users.LoggedUser;
using GraphQL.Types;

namespace ArchNet.Features.Users.Shared;

public sealed class UsersQuery : ObjectGraphType
{
    public UsersQuery(
        GetUserResolver getUserResolver,
        ListUsersResolver listUsersResolver,
        LoggedUserResolver loggedUserResolver)
    {
        foreach (var field in getUserResolver.Fields)
            AddField(field);

        foreach (var field in listUsersResolver.Fields)
            AddField(field);

        foreach (var field in loggedUserResolver.Fields)
            AddField(field);
    }
}
