using Library.Application.Contracts.Books;
using Library.Domain.Shared;
using MediatR;

namespace Library.Application.Books.Queries.GetBookById
{
    public sealed record GetBookByIdQuery(Guid Id) : IRequest<Result<BookResponse>>;
}
