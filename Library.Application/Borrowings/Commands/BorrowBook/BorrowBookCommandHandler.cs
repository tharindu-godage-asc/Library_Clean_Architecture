using Library.Application.Abstractions.Messaging;
using Library.Application.Contracts.Borrowings;
using Library.Application.Contracts.Mappings;
using Library.Application.Interfaces;
using Library.Domain.Entities;
using Library.Domain.Shared;
using Microsoft.Extensions.Logging;

namespace Library.Application.Borrowings.Commands.BorrowBook
{
    public sealed class BorrowBookCommandHandler
        : ICommandHandler<BorrowBookCommand, BorrowingResponse>
    {
        private readonly IBookRepository _bookRepository;
        private readonly IMemberRepository _memberRepository;
        private readonly IBorrowingRepository _borrowingRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<BorrowBookCommandHandler> _logger;

        public BorrowBookCommandHandler(
            IBookRepository bookRepository,
            IMemberRepository memberRepository,
            IBorrowingRepository borrowingRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            ILogger<BorrowBookCommandHandler> logger)
        {
            _bookRepository = bookRepository;
            _memberRepository = memberRepository;
            _borrowingRepository = borrowingRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<Result<BorrowingResponse>> Handle(
            BorrowBookCommand request,
            CancellationToken cancellationToken)
        {
            if (!_currentUser.IsAdmin && _currentUser.MemberId != request.MemberId)
            {
                _logger.LogWarning(
                    "Borrow rejected: caller {CallerMemberId} attempted to borrow for member {MemberId}",
                    _currentUser.MemberId,
                    request.MemberId);
                return Result.Failure<BorrowingResponse>(DomainErrors.Auth.Forbidden);
            }

            var member = await _memberRepository.GetByIdAsync(request.MemberId, cancellationToken);

            if (member is null)
            {
                _logger.LogWarning(
                    "Borrow rejected: member {MemberId} not found",
                    request.MemberId);
                return Result.Failure<BorrowingResponse>(DomainErrors.Member.NotFound(request.MemberId));
            }

            var book = await _bookRepository.GetByIdAsync(request.BookId, cancellationToken);

            if (book is null)
            {
                _logger.LogWarning(
                    "Borrow rejected: book {BookId} not found",
                    request.BookId);
                return Result.Failure<BorrowingResponse>(DomainErrors.Book.NotFound(request.BookId));
            }

            var activeBorrowings = await _borrowingRepository.CountActiveForMemberAsync(request.MemberId, cancellationToken);

            var eligibility = member.EnsureCanBorrow(activeBorrowings);

            if (eligibility.IsFailure)
            {
                _logger.LogWarning(
                    "Borrow rejected for member {MemberId}, book {BookId}: {ErrorCode}",
                    request.MemberId,
                    request.BookId,
                    eligibility.Error.Code);
                return Result.Failure<BorrowingResponse>(eligibility.Error);
            }

            var borrowCopyResult = book.BorrowCopy();

            if (borrowCopyResult.IsFailure)
            {
                _logger.LogWarning(
                    "Borrow rejected for member {MemberId}, book {BookId}: {ErrorCode}",
                    request.MemberId,
                    request.BookId,
                    borrowCopyResult.Error.Code);
                return Result.Failure<BorrowingResponse>(borrowCopyResult.Error);
            }

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

            _logger.LogInformation(
                "Book {BookId} borrowed by member {MemberId}, due {DueDate}",
                book.Id,
                member.Id,
                borrowing.DueDate);

            return Result.Success(borrowing.ToResponse());
        }
    }
}
