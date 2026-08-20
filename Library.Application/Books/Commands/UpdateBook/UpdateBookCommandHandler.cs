using Library.Application.Abstractions.Messaging;
using Library.Application.Contracts.Books;
using Library.Application.Contracts.Mappings;
using Library.Application.Interfaces;
using Library.Domain.Shared;
using Microsoft.Extensions.Logging;

namespace Library.Application.Books.Commands.UpdateBook
{
    public sealed class UpdateBookCommandHandler
        : ICommandHandler<UpdateBookCommand, BookResponse>
    {
        private readonly IBookRepository _bookRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateBookCommandHandler> _logger;

        public UpdateBookCommandHandler(
            IBookRepository bookRepository,
            IUnitOfWork unitOfWork,
            ILogger<UpdateBookCommandHandler> logger)
        {
            _bookRepository = bookRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<BookResponse>> Handle(
            UpdateBookCommand request,
            CancellationToken cancellationToken)
        {
            var book = await _bookRepository.GetByIdAsync(request.Id, cancellationToken);

            if (book is null)
            {
                _logger.LogWarning(
                    "Book update rejected: book {BookId} not found",
                    request.Id);
                return Result.Failure<BookResponse>(DomainErrors.Book.NotFound(request.Id));
            }

            var existingBookWithIsbn = await _bookRepository.GetByIsbnAsync(request.Isbn, cancellationToken);

            if (existingBookWithIsbn is not null && existingBookWithIsbn.Id != request.Id)
            {
                _logger.LogWarning(
                    "Book update rejected for book {BookId}: Isbn {Isbn} already exists",
                    request.Id,
                    request.Isbn);
                return Result.Failure<BookResponse>(DomainErrors.Book.IsbnAlreadyExists);
            }

            var updateResult = book.UpdateDetails(
                request.Title,
                request.Author,
                request.Isbn,
                request.PublishedYear,
                request.TotalCopies);

            if (updateResult.IsFailure)
            {
                _logger.LogWarning(
                    "Book update rejected for book {BookId}: {ErrorCode}",
                    request.Id,
                    updateResult.Error.Code);
                return Result.Failure<BookResponse>(updateResult.Error);
            }

            _bookRepository.Update(book);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Book {BookId} updated",
                book.Id);

            return Result.Success(book.ToResponse());
        }
    }
}
