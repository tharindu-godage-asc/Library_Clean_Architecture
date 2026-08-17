using Library.Domain.Entities;

namespace Library.Application.Interfaces
{
    public interface IBookRepository
    {
        Task<Book?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<Book?> GetByIsbnAsync(string isbn, CancellationToken cancellationToken = default);

        Task<IEnumerable<Book>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<IEnumerable<Book>> SearchAsync(
            string? title,
            string? author,
            int? publishedYear,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<Book>> SearchAsync(
            string? title,
            string? author,
            int? publishedYear,
            string? sortBy,
            bool sortDescending,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<int> CountAsync(
            string? title,
            string? author,
            int? publishedYear,
            CancellationToken cancellationToken = default);

        Task AddAsync(Book book, CancellationToken cancellationToken = default);

        void Update(Book book);

        void Delete(Book book);
    }
}