using Library.Application.Abstractions.Messaging;
using Library.Application.Contracts.Books;

namespace Library.Application.Books.Commands.UpdateBook
{
    public sealed record UpdateBookCommand(
        Guid Id,
        string Title,
        string Author,
        string Isbn,
        int PublishedYear,
        int TotalCopies) : ICommand<BookResponse>;
}
