using Library.Application.Contracts.Books;
using Library.Domain.Shared;
using MediatR;

namespace Library.Application.Books.Commands.UpdateBook
{
    public sealed record UpdateBookCommand(
        Guid Id,
        string Title,
        string Author,
        string Isbn,
        int PublishedYear,
        int TotalCopies) : IRequest<Result<BookResponse>>;
}
