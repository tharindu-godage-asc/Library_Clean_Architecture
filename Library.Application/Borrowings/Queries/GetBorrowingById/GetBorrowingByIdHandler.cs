using Library.Application.Contracts.Borrowings;
using Library.Application.Contracts.Mappings;
using Library.Application.Interfaces;
using Library.Domain.Shared;
using MediatR;

namespace Library.Application.Borrowings.Queries.GetBorrowingById
{
    public sealed class GetBorrowingByIdHandler
        : IRequestHandler<GetBorrowingByIdQuery, Result<BorrowingResponse>>
    {
        private readonly IBorrowingRepository _borrowingRepository;

        public GetBorrowingByIdHandler(IBorrowingRepository borrowingRepository)
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
