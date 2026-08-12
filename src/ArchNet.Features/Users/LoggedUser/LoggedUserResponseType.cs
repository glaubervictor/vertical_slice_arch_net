using ArchNet.Domain.Enums;
using ArchNet.Features.Users.Shared;
using GraphQL.Types;

namespace ArchNet.Features.Users.LoggedUser;

public sealed class LoggedUserResponseType : ObjectGraphType<LoggedUserResponse>
{
    public LoggedUserResponseType()
    {
        Name = nameof(LoggedUserResponseType);
        
        Field(r => r.Id);
        Field(r => r.Name);
        Field<NonNullGraphType<UserRoleType>, UserRole>("role")
            .Resolve(ctx => ctx.Source.Role);
    }
}
