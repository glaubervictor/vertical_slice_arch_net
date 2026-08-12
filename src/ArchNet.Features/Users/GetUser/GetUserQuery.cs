using ArchNet.Common.ResultPattern;
using Mediator;

namespace ArchNet.Features.Users.GetUser;

public record GetUserQuery(string Id) : IQuery<Result<GetUserResponse>>;
