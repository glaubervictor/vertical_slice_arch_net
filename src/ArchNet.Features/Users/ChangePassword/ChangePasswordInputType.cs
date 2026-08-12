using GraphQL.Types;

namespace ArchNet.Features.Users.ChangePassword;

public sealed class ChangePasswordInputType : InputObjectGraphType<ChangePasswordInput>
{
    public ChangePasswordInputType()
    {
        Field(i => i.UserId);
        Field(i => i.CurrentPassword);
        Field(i => i.NewPassword);
    }
}

public record ChangePasswordInput(string UserId, string CurrentPassword, string NewPassword);
