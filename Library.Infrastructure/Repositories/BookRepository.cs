using Library.Application.Interfaces;
using Library.Domain.Entities;
using Library.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Repositories
{
    public class BookRepository : IBookRepository
    {
        private readonly LibraryDbContext _context;

        public BookRepository(LibraryDbContext context)
        {
            _context = context;
        }

        public async Task<Book?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Books
                .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        }

        public async Task<Book?> GetByIsbnAsync(string isbn, CancellationToken cancellationToken = default)
        {
            return await _context.Books
                .FirstOrDefaultAsync(b => b.Isbn == isbn, cancellationToken);
        }

        public async Task<IEnumerable<Book>> SearchAsync(
            string? title,
            string? author,
            int? publishedYear,
            string? sortBy,
            bool sortDescending,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query = BuildFilteredQuery(title, author, publishedYear);

            query = sortBy?.ToLowerInvariant() switch
            {
                "author" => sortDescending
                    ? query.OrderByDescending(b => b.Author)
                    : query.OrderBy(b => b.Author),
                "publishedyear" => sortDescending
                    ? query.OrderByDescending(b => b.PublishedYear)
                    : query.OrderBy(b => b.PublishedYear),
                _ => sortDescending
                    ? query.OrderByDescending(b => b.Title)
                    : query.OrderBy(b => b.Title),
            };

            return await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> CountAsync(
            string? title,
            string? author,
            int? publishedYear,
            CancellationToken cancellationToken = default)
        {
            return await BuildFilteredQuery(title, author, publishedYear)
                .CountAsync(cancellationToken);
        }

        private IQueryable<Book> BuildFilteredQuery(string? title, string? author, int? publishedYear)
        {
            IQueryable<Book> query = _context.Books.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(title))
                query = query.Where(b => EF.Functions.ILike(b.Title, $"%{title}%"));

            if (!string.IsNullOrWhiteSpace(author))
                query = query.Where(b => EF.Functions.ILike(b.Author, $"%{author}%"));

            if (publishedYear.HasValue)
                query = query.Where(b => b.PublishedYear == publishedYear.Value);

            return query;
        }

        public async Task AddAsync(Book book, CancellationToken cancellationToken = default)
        {
            await _context.Books.AddAsync(book, cancellationToken);
        }

        public void Update(Book book)
        {
            _context.Books.Update(book);
        }

        public void Delete(Book book)
        {
            _context.Books.Remove(book);
        }
    }
}