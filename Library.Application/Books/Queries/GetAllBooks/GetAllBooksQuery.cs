using Library.Application.Abstractions.Messaging;
using Library.Application.Contracts.Books;
using Library.Application.Contracts.Common;

namespace Library.Application.Books.Queries.GetAllBooks
{
    public sealed record GetAllBooksQuery(
        string? Title,
        string? Author,
        int? PublishedYear,
        string? SortBy,
        bool SortDescending,
        int PageNumber,
        int PageSize) : IQuery<PagedResponse<BookResponse>>;
}
