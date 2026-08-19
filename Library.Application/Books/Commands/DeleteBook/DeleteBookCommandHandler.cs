using Library.Application.Abstractions.Messaging;
using Library.Application.Interfaces;
using Library.Domain.Shared;
using Microsoft.Extensions.Logging;

namespace Library.Application.Books.Commands.DeleteBook
{
    public sealed class DeleteBookCommandHandler : ICommandHandler<DeleteBookCommand>
    {
        private readonly IBookRepository _bookRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteBookCommandHandler> _logger;

        public DeleteBookCommandHandler(
            IBookRepository bookRepository,
            IUnitOfWork unitOfWork,
            ILogger<DeleteBookCommandHandler> logger)
        {
            _bookRepository = bookRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(
            DeleteBookCommand request,
            CancellationToken cancellationToken)
        {
            var book = await _bookRepository.GetByIdAsync(request.Id, cancellationToken);

            if (book is null)
            {
                _logger.LogWarning(
                    "Book deletion rejected: book {BookId} not found",
                    request.Id);
                return Result.Failure(DomainErrors.Book.NotFound(request.Id));
            }

            _bookRepository.Delete(book);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Book {BookId} deleted",
                request.Id);

            return Result.Success();
        }
    }
}
