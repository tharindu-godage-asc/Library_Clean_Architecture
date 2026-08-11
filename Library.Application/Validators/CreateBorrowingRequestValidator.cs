using FluentValidation;
using Library.Application.Contracts.Borrowings;

namespace Library.Application.Validators
{
    public class CreateBorrowingRequestValidator
        : AbstractValidator<CreateBorrowingRequest>
    {
        public CreateBorrowingRequestValidator()
        {
            RuleFor(x => x.BookId)
                .GreaterThan(0);

            RuleFor(x => x.MemberId)
                .GreaterThan(0);
        }
    }
}
