using FluentValidation;
using Library.Application.Contracts.Members;

namespace Library.Application.Validators
{
    public class UpdateMemberRequestValidator
        : AbstractValidator<UpdateMemberRequest>
    {
        public UpdateMemberRequestValidator()
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
