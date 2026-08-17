using Library.Application.Interfaces;
using Library.Domain.Shared;
using MediatR;

namespace Library.Application.Borrowings.Commands.ReturnBook
{
    public sealed class ReturnBookHandler : IRequestHandler<ReturnBookCommand, Result>
    {
        private readonly IBookRepository _bookRepository;
        private readonly IBorrowingRepository _borrowingRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ReturnBookHandler(
            IBookRepository bookRepository,
            IBorrowingRepository borrowingRepository,
            IUnitOfWork unitOfWork)
        {
            _bookRepository = bookRepository;
            _borrowingRepository = borrowingRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(
            ReturnBookCommand request,
            CancellationToken cancellationToken)
        {
            var borrowing = await _borrowingRepository.GetByIdAsync(request.BorrowingId, cancellationToken);

            if (borrowing is null)
                return Result.Failure(DomainErrors.Borrowing.NotFound(request.BorrowingId));

            var book = await _bookRepository.GetByIdAsync(borrowing.BookId, cancellationToken);

            if (book is null)
                return Result.Failure(DomainErrors.Book.NotFound(borrowing.BookId));

            var returnBorrowingResult = borrowing.ReturnBook();

            if (returnBorrowingResult.IsFailure)
                return Result.Failure(returnBorrowingResult.Error);

            var returnCopyResult = book.ReturnCopy();

            if (returnCopyResult.IsFailure)
                return Result.Failure(returnCopyResult.Error);

            _borrowingRepository.Update(borrowing);
            _bookRepository.Update(book);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
