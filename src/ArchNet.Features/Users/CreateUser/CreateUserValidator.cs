using FluentValidation;

namespace ArchNet.Features.Users.CreateUser;

public class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Login).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Password).NotEmpty().MinimumLength(8);
    }
}
