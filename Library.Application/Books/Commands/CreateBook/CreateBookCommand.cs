using Library.Application.Contracts.Books;
using Library.Domain.Shared;
using MediatR;

namespace Library.Application.Books.Commands.CreateBook
{
    public sealed record CreateBookCommand(
        string Title,
        string Author,
        string Isbn,
        int PublishedYear,
        int TotalCopies) : IRequest<Result<BookResponse>>;
}
