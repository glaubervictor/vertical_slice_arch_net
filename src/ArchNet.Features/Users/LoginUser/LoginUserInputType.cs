using GraphQL.Types;

namespace ArchNet.Features.Users.LoginUser;

public sealed class LoginUserInputType : InputObjectGraphType<LoginUserInput>
{
    public LoginUserInputType()
    {
        Field(i => i.Login);
        Field(i => i.Password);
    }
}

public record LoginUserInput(string Login, string Password);
