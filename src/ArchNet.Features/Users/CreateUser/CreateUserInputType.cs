using ArchNet.Domain.Enums;
using ArchNet.Features.Users.Shared;
using GraphQL.Types;

namespace ArchNet.Features.Users.CreateUser;

public sealed class CreateUserInputType : InputObjectGraphType<CreateUserInput>
{
    public CreateUserInputType()
    {
        Field(i => i.Name);
        Field(i => i.Login);
        Field(i => i.Password);
        Field(i => i.Role, typeof(NonNullGraphType<UserRoleType>));
    }
}

public record CreateUserInput(string Name, string Login, string Password, UserRole Role);
