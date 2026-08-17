using Library.Application.Contracts.Books;
using Library.Application.Contracts.Mappings;
using Library.Application.Interfaces;
using Library.Domain.Shared;
using MediatR;

namespace Library.Application.Books.Queries.GetBookById
{
    public sealed class GetBookByIdHandler
        : IRequestHandler<GetBookByIdQuery, Result<BookResponse>>
    {
        private readonly IBookRepository _bookRepository;

        public GetBookByIdHandler(IBookRepository bookRepository)
        {
            _bookRepository = bookRepository;
        }

        public async Task<Result<BookResponse>> Handle(
            GetBookByIdQuery request,
            CancellationToken cancellationToken)
        {
            var book = await _bookRepository.GetByIdAsync(request.Id, cancellationToken);

            return book is null
                ? Result.Failure<BookResponse>(DomainErrors.Book.NotFound(request.Id))
                : Result.Success(book.ToResponse());
        }
    }
}
