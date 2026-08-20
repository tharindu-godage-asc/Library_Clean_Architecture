using Library.Application.Abstractions.Messaging;
using Library.Application.Interfaces;
using Library.Domain.Shared;
using Microsoft.Extensions.Logging;

namespace Library.Application.Borrowings.Commands.ReturnBook
{
    public sealed class ReturnBookCommandHandler : ICommandHandler<ReturnBookCommand>
    {
        private readonly IBookRepository _bookRepository;
        private readonly IBorrowingRepository _borrowingRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ReturnBookCommandHandler> _logger;

        public ReturnBookCommandHandler(
            IBookRepository bookRepository,
            IBorrowingRepository borrowingRepository,
            IUnitOfWork unitOfWork,
            ILogger<ReturnBookCommandHandler> logger)
        {
            _bookRepository = bookRepository;
            _borrowingRepository = borrowingRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(
            ReturnBookCommand request,
            CancellationToken cancellationToken)
        {
            var borrowing = await _borrowingRepository.GetByIdAsync(request.BorrowingId, cancellationToken);

            if (borrowing is null)
            {
                _logger.LogWarning(
                    "Return rejected: borrowing {BorrowingId} not found",
                    request.BorrowingId);
                return Result.Failure(DomainErrors.Borrowing.NotFound(request.BorrowingId));
            }

            var book = await _bookRepository.GetByIdAsync(borrowing.BookId, cancellationToken);

            if (book is null)
            {
                _logger.LogWarning(
                    "Return rejected: book {BookId} not found for borrowing {BorrowingId}",
                    borrowing.BookId,
                    request.BorrowingId);
                return Result.Failure(DomainErrors.Book.NotFound(borrowing.BookId));
            }

            var returnBorrowingResult = borrowing.ReturnBook();

            if (returnBorrowingResult.IsFailure)
            {
                _logger.LogWarning(
                    "Return rejected for borrowing {BorrowingId}: {ErrorCode}",
                    request.BorrowingId,
                    returnBorrowingResult.Error.Code);
                return Result.Failure(returnBorrowingResult.Error);
            }

            var returnCopyResult = book.ReturnCopy();

            if (returnCopyResult.IsFailure)
                return Result.Failure(returnCopyResult.Error);

            _borrowingRepository.Update(borrowing);
            _bookRepository.Update(book);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Book {BookId} returned for borrowing {BorrowingId}",
                book.Id,
                borrowing.Id);

            return Result.Success();
        }
    }
}
