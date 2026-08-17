using Library.Application.Contracts.Borrowings;
using Library.Domain.Shared;
using MediatR;

namespace Library.Application.Borrowings.Queries.GetBorrowingById
{
    public sealed record GetBorrowingByIdQuery(Guid Id) : IRequest<Result<BorrowingResponse>>;
}
