using ArchNet.Common.ResultPattern;
using Mediator;

namespace ArchNet.Features.Users.LoginUser;

public record LoginUserCommand(string Login, string Password)
    : ICommand<Result<LoginUserResponse>>;
