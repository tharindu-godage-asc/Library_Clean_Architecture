using Library.Application.Contracts.Books;
using Library.Application.Contracts.Mappings;
using Library.Application.Interfaces;
using MediatR;

namespace Library.Application.Books.Queries.GetAllBooks
{
    public sealed class GetAllBooksHandler
        : IRequestHandler<GetAllBooksQuery, IEnumerable<BookResponse>>
    {
        private readonly IBookRepository _bookRepository;

        public GetAllBooksHandler(IBookRepository bookRepository)
        {
            _bookRepository = bookRepository;
        }

        public async Task<IEnumerable<BookResponse>> Handle(
            GetAllBooksQuery request,
            CancellationToken cancellationToken)
        {
            var books = await _bookRepository.SearchAsync(
                request.Title,
                request.Author,
                request.PublishedYear,
                cancellationToken);

            return books.Select(b => b.ToResponse());
        }
    }
}
