using Library.Application.Abstractions.Messaging;
using Library.Application.Contracts.Books;
using Library.Domain.Shared;

namespace Library.Application.Books.Queries.GetBookById
{
    public sealed record GetBookByIdQuery(Guid Id) : IQuery<Result<BookResponse>>;
}
