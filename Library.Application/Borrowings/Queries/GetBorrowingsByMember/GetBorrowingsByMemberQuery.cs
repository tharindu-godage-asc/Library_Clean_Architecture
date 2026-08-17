using Library.Application.Contracts.Borrowings;
using Library.Domain.Shared;
using MediatR;

namespace Library.Application.Borrowings.Queries.GetBorrowingsByMember
{
    public sealed record GetBorrowingsByMemberQuery(Guid MemberId) : IRequest<Result<IEnumerable<BorrowingResponse>>>;
}
