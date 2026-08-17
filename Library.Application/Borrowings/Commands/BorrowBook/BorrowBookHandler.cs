using Library.Application.Contracts.Borrowings;
using Library.Application.Contracts.Mappings;
using Library.Application.Interfaces;
using Library.Domain.Entities;
using Library.Domain.Shared;
using MediatR;

namespace Library.Application.Borrowings.Commands.BorrowBook
{
    public sealed class BorrowBookHandler
        : IRequestHandler<BorrowBookCommand, Result<BorrowingResponse>>
    {
        private const int MaxActiveBorrowingsPerMember = 3;
        private static readonly TimeSpan LoanPeriod = TimeSpan.FromDays(14);

        private readonly IBookRepository _bookRepository;
        private readonly IMemberRepository _memberRepository;
        private readonly IBorrowingRepository _borrowingRepository;
        private readonly IUnitOfWork _unitOfWork;

        public BorrowBookHandler(
            IBookRepository bookRepository,
            IMemberRepository memberRepository,
            IBorrowingRepository borrowingRepository,
            IUnitOfWork unitOfWork)
        {
            _bookRepository = bookRepository;
            _memberRepository = memberRepository;
            _borrowingRepository = borrowingRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<BorrowingResponse>> Handle(
            BorrowBookCommand request,
            CancellationToken cancellationToken)
        {
            var member = await _memberRepository.GetByIdAsync(request.MemberId);

            if (member is null)
                return Result.Failure<BorrowingResponse>(DomainErrors.Member.NotFound(request.MemberId));

            var book = await _bookRepository.GetByIdAsync(request.BookId);

            if (book is null)
                return Result.Failure<BorrowingResponse>(DomainErrors.Book.NotFound(request.BookId));

            if (!member.IsActive)
                return Result.Failure<BorrowingResponse>(DomainErrors.Member.Inactive);

            var activeBorrowings = await _borrowingRepository.CountActiveForMemberAsync(request.MemberId);

            if (activeBorrowings >= MaxActiveBorrowingsPerMember)
                return Result.Failure<BorrowingResponse>(DomainErrors.Borrowing.LimitExceeded);

            var borrowCopyResult = book.BorrowCopy();

            if (borrowCopyResult.IsFailure)
                return Result.Failure<BorrowingResponse>(borrowCopyResult.Error);

            var borrowedAt = DateTime.UtcNow;

            var borrowingResult = Borrowing.Create(
                request.BookId,
                request.MemberId,
                borrowedAt,
                borrowedAt.Add(LoanPeriod));

            if (borrowingResult.IsFailure)
                return Result.Failure<BorrowingResponse>(borrowingResult.Error);

            var borrowing = borrowingResult.Value;

            await _borrowingRepository.AddAsync(borrowing);
            _bookRepository.Update(book);

            await _unitOfWork.SaveChangesAsync();

            return Result.Success(borrowing.ToResponse());
        }
    }
}
