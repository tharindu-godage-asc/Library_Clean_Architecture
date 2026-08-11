using Library.Domain.Entities;

namespace Library.Application.Interfaces
{
    public interface IBorrowingRepository
    {
        Task<Borrowing?> GetByIdAsync(int id);

        Task<IEnumerable<Borrowing>> GetAllAsync();

        Task<IEnumerable<Borrowing>> GetActiveBorrowingsAsync();

        Task<int> CountActiveForMemberAsync(int memberId);

        Task AddAsync(Borrowing borrowing);

        void Update(Borrowing borrowing);
    }
}