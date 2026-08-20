using Library.Application.Abstractions.Messaging;
using Library.Application.Contracts.Borrowings;
using Library.Application.Contracts.Mappings;
using Library.Application.Interfaces;
using Library.Domain.Shared;

namespace Library.Application.Borrowings.Queries.GetBorrowingById
{
    public sealed class GetBorrowingByIdQueryHandler
        : IQueryHandler<GetBorrowingByIdQuery, Result<BorrowingResponse>>
    {
        private readonly IBorrowingRepository _borrowingRepository;

        public GetBorrowingByIdQueryHandler(IBorrowingRepository borrowingRepository)
        {
            _borrowingRepository = borrowingRepository;
        }

        public async Task<Result<BorrowingResponse>> Handle(
            GetBorrowingByIdQuery request,
            CancellationToken cancellationToken)
        {
            var borrowing = await _borrowingRepository.GetByIdAsync(request.Id, cancellationToken);

            return borrowing is null
                ? Result.Failure<BorrowingResponse>(DomainErrors.Borrowing.NotFound(request.Id))
                : Result.Success(borrowing.ToResponse());
        }
    }
}
