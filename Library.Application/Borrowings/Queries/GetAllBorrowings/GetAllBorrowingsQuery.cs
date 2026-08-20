using Library.Application.Abstractions.Messaging;
using Library.Application.Contracts.Borrowings;

namespace Library.Application.Borrowings.Queries.GetAllBorrowings
{
    public sealed record GetAllBorrowingsQuery : IQuery<IEnumerable<BorrowingResponse>>;
}
