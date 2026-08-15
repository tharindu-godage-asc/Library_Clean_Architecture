using Library.Application.Contracts.Books;
using Library.Application.Contracts.Mappings;
using Library.Application.Interfaces;
using Library.Domain.Entities;
using Library.Domain.Shared;
using MediatR;

namespace Library.Application.Books.Commands.CreateBook
{
    public sealed class CreateBookHandler
        : IRequestHandler<CreateBookCommand, Result<BookResponse>>
    {
        private readonly IBookRepository _bookRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateBookHandler(
            IBookRepository bookRepository,
            IUnitOfWork unitOfWork)
        {
            _bookRepository = bookRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<BookResponse>> Handle(
            CreateBookCommand request,
            CancellationToken cancellationToken)
        {
            var existingBook = await _bookRepository.GetByIsbnAsync(request.Isbn);

            if (existingBook is not null)
                return Result.Failure<BookResponse>(DomainErrors.Book.IsbnAlreadyExists);

            var bookResult = Book.Create(
                request.Title,
                request.Author,
                request.Isbn,
                request.PublishedYear,
                request.TotalCopies);

            if (bookResult.IsFailure)
                return Result.Failure<BookResponse>(bookResult.Error);

            var book = bookResult.Value;

            await _bookRepository.AddAsync(book);
            await _unitOfWork.SaveChangesAsync();

            return Result.Success(book.ToResponse());
        }
    }
}
