using Library.Domain.Entities;

namespace Library.Application.Interfaces
{
    public interface IBorrowingRepository
    {
        Task<Borrowing?> GetByIdAsync(Guid id);

        Task<IEnumerable<Borrowing>> GetAllAsync();

        Task<IEnumerable<Borrowing>> GetActiveBorrowingsAsync();

        Task<int> CountActiveForMemberAsync(Guid memberId);

        Task AddAsync(Borrowing borrowing);

        void Update(Borrowing borrowing);
    }
}