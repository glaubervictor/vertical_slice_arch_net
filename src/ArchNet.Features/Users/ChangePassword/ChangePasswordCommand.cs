using ArchNet.Common.ResultPattern;
using ArchNet.Features.Users.GetUser;
using Mediator;

namespace ArchNet.Features.Users.ChangePassword;

public record ChangePasswordCommand(string UserId, string CurrentPassword, string NewPassword)
    : ICommand<Result<GetUserResponse>>;
