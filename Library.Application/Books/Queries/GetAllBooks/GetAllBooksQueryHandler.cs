using Library.Application.Contracts.Books;
using Library.Application.Contracts.Common;
using Library.Application.Contracts.Mappings;
using Library.Application.Interfaces;
using MediatR;

namespace Library.Application.Books.Queries.GetAllBooks
{
    public sealed class GetAllBooksQueryHandler
        : IRequestHandler<GetAllBooksQuery, PagedResponse<BookResponse>>
    {
        private const int MaxPageSize = 100;

        private readonly IBookRepository _bookRepository;

        public GetAllBooksQueryHandler(IBookRepository bookRepository)
        {
            _bookRepository = bookRepository;
        }

        public async Task<PagedResponse<BookResponse>> Handle(
            GetAllBooksQuery request,
            CancellationToken cancellationToken)
        {
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1
                ? 1
                : Math.Min(request.PageSize, MaxPageSize);

            var books = await _bookRepository.SearchAsync(
                request.Title,
                request.Author,
                request.PublishedYear,
                request.SortBy,
                request.SortDescending,
                pageNumber,
                pageSize,
                cancellationToken);

            var totalCount = await _bookRepository.CountAsync(
                request.Title,
                request.Author,
                request.PublishedYear,
                cancellationToken);

            return new PagedResponse<BookResponse>
            {
                Items = books.Select(b => b.ToResponse()),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }
    }
}
