using Library.Application.Abstractions.Messaging;
using Library.Application.Contracts.Borrowings;
using Library.Domain.Shared;

namespace Library.Application.Borrowings.Queries.GetBorrowingsByMember
{
    public sealed record GetBorrowingsByMemberQuery(Guid MemberId) : IQuery<Result<IEnumerable<BorrowingResponse>>>;
}
