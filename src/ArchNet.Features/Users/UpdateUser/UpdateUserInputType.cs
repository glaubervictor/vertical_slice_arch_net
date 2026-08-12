using ArchNet.Domain.Enums;
using ArchNet.Features.Users.Shared;
using GraphQL.Types;

namespace ArchNet.Features.Users.UpdateUser;

public class UpdateUserInputType : InputObjectGraphType<UpdateUserInput>
{
    public UpdateUserInputType()
    {
        Name = "UpdateUserInput";
        Field(i => i.Id);
        Field(i => i.Name);
        Field(i => i.Role, typeof(NonNullGraphType<UserRoleType>));
    }
}

public record UpdateUserInput(string Id, string Name, UserRole Role);
