using ArchNet.Common.ResultPattern;
using ArchNet.Domain.Enums;
using ArchNet.Features.Users.GetUser;
using Mediator;

namespace ArchNet.Features.Users.CreateUser;

public record CreateUserCommand(string Name, string Login, string Password, UserRole Role)
    : ICommand<Result<GetUserResponse>>;
