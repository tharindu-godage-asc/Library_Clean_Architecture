using Library.Application.Contracts.Borrowings;
using MediatR;

namespace Library.Application.Borrowings.Queries.GetAllBorrowings
{
    public sealed record GetAllBorrowingsQuery : IRequest<IEnumerable<BorrowingResponse>>;
}
