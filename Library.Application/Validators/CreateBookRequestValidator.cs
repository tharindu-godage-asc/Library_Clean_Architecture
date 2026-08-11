using FluentValidation;
using Library.Application.Contracts.Books;

namespace Library.Application.Validators
{
    public class CreateBookRequestValidator
        : AbstractValidator<CreateBookRequest>
    {
        public CreateBookRequestValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(x => x.Author)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.Isbn)
                .NotEmpty()
                .MaximumLength(20);

            RuleFor(x => x.PublishedYear)
                .LessThanOrEqualTo(DateTime.UtcNow.Year);

            RuleFor(x => x.TotalCopies)
                .GreaterThan(0);
        }
    }
}