using ArchNet.Domain.Enums;
using ArchNet.Features.Users.GetUser;
using GraphQL.Types;

namespace ArchNet.Features.Users.Shared;

public sealed class UserType : ObjectGraphType<GetUserResponse>
{
    public UserType()
    {
        Name = nameof(UserType);
        
        Field(u => u.Id);
        Field(u => u.Name);
        Field(u => u.Login);
        Field<NonNullGraphType<UserRoleType>, UserRole>("role")
            .Resolve(ctx => ctx.Source.Role);
    }
}
