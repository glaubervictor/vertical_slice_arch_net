using ArchNet.Common.ResultPattern;
using ArchNet.Domain.Enums;
using ArchNet.Features.Users.GetUser;
using Mediator;

namespace ArchNet.Features.Users.UpdateUser;

public record UpdateUserCommand(string Id, string Name, UserRole Role)
    : ICommand<Result<GetUserResponse>>;
