using Library.Application.Contracts.Books;
using Library.Application.Contracts.Mappings;
using Library.Application.Interfaces;
using Library.Domain.Shared;
using MediatR;

namespace Library.Application.Books.Commands.UpdateBook
{
    public sealed class UpdateBookCommandHandler
        : IRequestHandler<UpdateBookCommand, Result<BookResponse>>
    {
        private readonly IBookRepository _bookRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateBookCommandHandler(
            IBookRepository bookRepository,
            IUnitOfWork unitOfWork)
        {
            _bookRepository = bookRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<BookResponse>> Handle(
            UpdateBookCommand request,
            CancellationToken cancellationToken)
        {
            var book = await _bookRepository.GetByIdAsync(request.Id, cancellationToken);

            if (book is null)
                return Result.Failure<BookResponse>(DomainErrors.Book.NotFound(request.Id));

            var existingBookWithIsbn = await _bookRepository.GetByIsbnAsync(request.Isbn, cancellationToken);

            if (existingBookWithIsbn is not null && existingBookWithIsbn.Id != request.Id)
                return Result.Failure<BookResponse>(DomainErrors.Book.IsbnAlreadyExists);

            var updateResult = book.UpdateDetails(
                request.Title,
                request.Author,
                request.Isbn,
                request.PublishedYear,
                request.TotalCopies);

            if (updateResult.IsFailure)
                return Result.Failure<BookResponse>(updateResult.Error);

            _bookRepository.Update(book);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(book.ToResponse());
        }
    }
}
