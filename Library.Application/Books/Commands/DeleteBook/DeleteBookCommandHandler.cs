using Library.Application.Abstractions.Messaging;
using Library.Application.Interfaces;
using Library.Domain.Shared;

namespace Library.Application.Books.Commands.DeleteBook
{
    public sealed class DeleteBookCommandHandler : ICommandHandler<DeleteBookCommand>
    {
        private readonly IBookRepository _bookRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteBookCommandHandler(
            IBookRepository bookRepository,
            IUnitOfWork unitOfWork)
        {
            _bookRepository = bookRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(
            DeleteBookCommand request,
            CancellationToken cancellationToken)
        {
            var book = await _bookRepository.GetByIdAsync(request.Id, cancellationToken);

            if (book is null)
                return Result.Failure(DomainErrors.Book.NotFound(request.Id));

            _bookRepository.Delete(book);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
