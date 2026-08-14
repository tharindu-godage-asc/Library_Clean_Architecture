using Library.Domain.Entities;

namespace Library.Application.Interfaces
{
    public interface IBookRepository
    {
        Task<Book?> GetByIdAsync(Guid id);

        Task<Book?> GetByIsbnAsync(string isbn);

        Task<IEnumerable<Book>> GetAllAsync();

        Task AddAsync(Book book);

        void Update(Book book);

        void Delete(Book book);
    }
}