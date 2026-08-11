using FluentValidation;
using Library.Application.Contracts.Members;

namespace Library.Application.Validators
{
    public class CreateMemberRequestValidator
        : AbstractValidator<CreateMemberRequest>
    {
        public CreateMemberRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress();

            RuleFor(x => x.PhoneNumber)
                .NotEmpty()
                .MaximumLength(20);
        }
    }
}