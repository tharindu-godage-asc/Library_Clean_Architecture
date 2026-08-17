using Library.Domain.Entities;

namespace Library.Application.Interfaces
{
    public interface IBorrowingRepository
    {
        Task<Borrowing?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<IEnumerable<Borrowing>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<IEnumerable<Borrowing>> GetActiveBorrowingsAsync(CancellationToken cancellationToken = default);

        Task<int> CountActiveForMemberAsync(Guid memberId, CancellationToken cancellationToken = default);

        Task AddAsync(Borrowing borrowing, CancellationToken cancellationToken = default);

        void Update(Borrowing borrowing);
    }
}