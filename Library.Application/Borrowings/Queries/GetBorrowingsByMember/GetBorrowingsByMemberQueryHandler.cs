using Library.Application.Contracts.Borrowings;
using Library.Application.Contracts.Mappings;
using Library.Application.Interfaces;
using Library.Domain.Shared;
using MediatR;

namespace Library.Application.Borrowings.Queries.GetBorrowingsByMember
{
    public sealed class GetBorrowingsByMemberQueryHandler
        : IRequestHandler<GetBorrowingsByMemberQuery, Result<IEnumerable<BorrowingResponse>>>
    {
        private readonly IMemberRepository _memberRepository;
        private readonly IBorrowingRepository _borrowingRepository;

        public GetBorrowingsByMemberQueryHandler(
            IMemberRepository memberRepository,
            IBorrowingRepository borrowingRepository)
        {
            _memberRepository = memberRepository;
            _borrowingRepository = borrowingRepository;
        }

        public async Task<Result<IEnumerable<BorrowingResponse>>> Handle(
            GetBorrowingsByMemberQuery request,
            CancellationToken cancellationToken)
        {
            var member = await _memberRepository.GetByIdAsync(request.MemberId, cancellationToken);

            if (member is null)
                return Result.Failure<IEnumerable<BorrowingResponse>>(DomainErrors.Member.NotFound(request.MemberId));

            var borrowings = await _borrowingRepository.GetByMemberIdAsync(request.MemberId, cancellationToken);

            return Result.Success(borrowings.Select(b => b.ToResponse()));
        }
    }
}
