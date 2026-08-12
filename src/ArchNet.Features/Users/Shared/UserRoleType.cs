using ArchNet.Domain.Enums;
using GraphQL.Types;

namespace ArchNet.Features.Users.Shared;

public sealed class UserRoleType : EnumerationGraphType<UserRole>
{
    protected override string ChangeEnumCase(string val) => val;
}
