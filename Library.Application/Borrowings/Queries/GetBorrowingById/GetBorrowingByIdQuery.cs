using Library.Application.Abstractions.Messaging;
using Library.Application.Contracts.Borrowings;
using Library.Domain.Shared;

namespace Library.Application.Borrowings.Queries.GetBorrowingById
{
    public sealed record GetBorrowingByIdQuery(Guid Id) : IQuery<Result<BorrowingResponse>>;
}
