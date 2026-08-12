using FluentValidation;

namespace ArchNet.Features.Users.LoginUser;

public sealed class LoginUserValidator : AbstractValidator<LoginUserCommand>
{
    public LoginUserValidator()
    {
        RuleFor(c => c.Login).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Password).NotEmpty().MinimumLength(8);
    }
}
