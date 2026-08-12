using ArchNet.Features.Users.ChangePassword;
using ArchNet.Features.Users.CreateUser;
using ArchNet.Features.Users.DeleteUser;
using ArchNet.Features.Users.LoginUser;
using ArchNet.Features.Users.UpdateUser;
using GraphQL.Types;

namespace ArchNet.Features.Users.Shared;

public sealed class UsersMutation : ObjectGraphType
{
    public UsersMutation(
        LoginUserMutation loginUserMutation,
        CreateUserMutation createUserMutation,
        UpdateUserMutation updateUserMutation,
        DeleteUserMutation deleteUserMutation,
        ChangePasswordMutation changePasswordMutation)
    {
        foreach (var field in loginUserMutation.Fields)
            AddField(field);

        foreach (var field in createUserMutation.Fields)
            AddField(field);

        foreach (var field in updateUserMutation.Fields)
            AddField(field);

        foreach (var field in deleteUserMutation.Fields)
            AddField(field);

        foreach (var field in changePasswordMutation.Fields)
            AddField(field);
    }
}
