using ArchNet.Common.ResultPattern;
using Mediator;

namespace ArchNet.Features.Users.DeleteUser;

public record DeleteUserCommand(string Id) : ICommand<Result<DeleteUserResponse>>;

public record DeleteUserResponse(string Id);
