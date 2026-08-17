using Library.Application.Contracts.Books;
using MediatR;

namespace Library.Application.Books.Queries.GetAllBooks
{
    public sealed record GetAllBooksQuery(
        string? Title,
        string? Author,
        int? PublishedYear) : IRequest<IEnumerable<BookResponse>>;
}
