using ArchNet.Domain.Enums;
using ArchNet.Features.Users.Shared;
using GraphQL.Types;

namespace ArchNet.Features.Users.LoginUser;

public sealed class LoginUserResponseType : ObjectGraphType<LoginUserResponse>
{
    public LoginUserResponseType()
    {
        Name = nameof(LoginUserResponseType);
        
        Field(r => r.Token);
        Field(r => r.UserId);
        Field(r => r.Name);
        Field<NonNullGraphType<UserRoleType>, UserRole>("role")
            .Resolve(ctx => ctx.Source.Role);
    }
}
