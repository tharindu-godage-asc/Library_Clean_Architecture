using FluentValidation;
using Library.Application.Contracts.Auth;

namespace Library.Application.Validators
{
    public class LoginRequestValidator
        : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress();

            RuleFor(x => x.Password)
                .NotEmpty();
        }
    }
}
