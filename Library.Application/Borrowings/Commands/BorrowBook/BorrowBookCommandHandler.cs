using Library.Application.Abstractions.Messaging;
using Library.Application.Contracts.Borrowings;
using Library.Application.Contracts.Mappings;
using Library.Application.Interfaces;
using Library.Domain.Entities;
using Library.Domain.Shared;

namespace Library.Application.Borrowings.Commands.BorrowBook
{
    public sealed class BorrowBookCommandHandler
        : ICommandHandler<BorrowBookCommand, BorrowingResponse>
    {
        private readonly IBookRepository _bookRepository;
        private readonly IMemberRepository _memberRepository;
        private readonly IBorrowingRepository _borrowingRepository;
        private readonly IUnitOfWork _unitOfWork;

        public BorrowBookCommandHandler(
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
            var member = await _memberRepository.GetByIdAsync(request.MemberId, cancellationToken);

            if (member is null)
                return Result.Failure<BorrowingResponse>(DomainErrors.Member.NotFound(request.MemberId));

            var book = await _bookRepository.GetByIdAsync(request.BookId, cancellationToken);

            if (book is null)
                return Result.Failure<BorrowingResponse>(DomainErrors.Book.NotFound(request.BookId));

            var activeBorrowings = await _borrowingRepository.CountActiveForMemberAsync(request.MemberId, cancellationToken);

            var eligibility = member.EnsureCanBorrow(activeBorrowings);

            if (eligibility.IsFailure)
                return Result.Failure<BorrowingResponse>(eligibility.Error);

            var borrowCopyResult = book.BorrowCopy();

            if (borrowCopyResult.IsFailure)
                return Result.Failure<BorrowingResponse>(borrowCopyResult.Error);

            var borrowingResult = Borrowing.CreateForLoan(
                request.BookId,
                request.MemberId,
                DateTime.UtcNow);

            if (borrowingResult.IsFailure)
                return Result.Failure<BorrowingResponse>(borrowingResult.Error);

            var borrowing = borrowingResult.Value;

            await _borrowingRepository.AddAsync(borrowing, cancellationToken);
            _bookRepository.Update(book);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(borrowing.ToResponse());
        }
    }
}
