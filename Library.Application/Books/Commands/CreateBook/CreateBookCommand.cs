using Library.Application.Abstractions.Messaging;
using Library.Application.Contracts.Books;

namespace Library.Application.Books.Commands.CreateBook
{
    public sealed record CreateBookCommand(
        string Title,
        string Author,
        string Isbn,
        int PublishedYear,
        int TotalCopies) : ICommand<BookResponse>;
}
