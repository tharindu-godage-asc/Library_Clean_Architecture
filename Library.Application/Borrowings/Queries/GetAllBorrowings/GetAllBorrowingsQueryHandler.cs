using Library.Application.Abstractions.Messaging;
using Library.Application.Contracts.Borrowings;
using Library.Application.Contracts.Mappings;
using Library.Application.Interfaces;

namespace Library.Application.Borrowings.Queries.GetAllBorrowings
{
    public sealed class GetAllBorrowingsQueryHandler
        : IQueryHandler<GetAllBorrowingsQuery, IEnumerable<BorrowingResponse>>
    {
        private readonly IBorrowingRepository _borrowingRepository;

        public GetAllBorrowingsQueryHandler(IBorrowingRepository borrowingRepository)
        {
            _borrowingRepository = borrowingRepository;
        }

        public async Task<IEnumerable<BorrowingResponse>> Handle(
            GetAllBorrowingsQuery request,
            CancellationToken cancellationToken)
        {
            var borrowings = await _borrowingRepository.GetAllAsync(cancellationToken);

            return borrowings.Select(b => b.ToResponse());
        }
    }
}
