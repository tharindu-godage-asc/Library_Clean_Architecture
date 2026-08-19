using Library.Application.Abstractions.Messaging;
using Library.Application.Contracts.Books;
using Library.Application.Contracts.Mappings;
using Library.Application.Interfaces;
using Library.Domain.Entities;
using Library.Domain.Shared;
using Microsoft.Extensions.Logging;

namespace Library.Application.Books.Commands.CreateBook
{
    public sealed class CreateBookCommandHandler
        : ICommandHandler<CreateBookCommand, BookResponse>
    {
        private readonly IBookRepository _bookRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateBookCommandHandler> _logger;

        public CreateBookCommandHandler(
            IBookRepository bookRepository,
            IUnitOfWork unitOfWork,
            ILogger<CreateBookCommandHandler> logger)
        {
            _bookRepository = bookRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<BookResponse>> Handle(
            CreateBookCommand request,
            CancellationToken cancellationToken)
        {
            var existingBook = await _bookRepository.GetByIsbnAsync(request.Isbn, cancellationToken);

            if (existingBook is not null)
            {
                _logger.LogWarning(
                    "Book creation rejected: Isbn {Isbn} already exists",
                    request.Isbn);
                return Result.Failure<BookResponse>(DomainErrors.Book.IsbnAlreadyExists);
            }

            var bookResult = Book.Create(
                request.Title,
                request.Author,
                request.Isbn,
                request.PublishedYear,
                request.TotalCopies);

            if (bookResult.IsFailure)
            {
                _logger.LogWarning(
                    "Book creation rejected for Isbn {Isbn}: {ErrorCode}",
                    request.Isbn,
                    bookResult.Error.Code);
                return Result.Failure<BookResponse>(bookResult.Error);
            }

            var book = bookResult.Value;

            await _bookRepository.AddAsync(book, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Book {BookId} created, Isbn {Isbn}",
                book.Id,
                book.Isbn);

            return Result.Success(book.ToResponse());
        }
    }
}
