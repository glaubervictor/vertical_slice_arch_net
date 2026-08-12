using ArchNet.Common.ResultPattern;
using Mediator;

namespace ArchNet.Features.Users.LoggedUser;

public record LoggedUserQuery() : IQuery<Result<LoggedUserResponse>>;
