using Library.Domain.Entities;

namespace Library.Application.Interfaces
{
    public interface IMemberRepository
    {
        Task<Member?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<Member?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

        Task<IEnumerable<Member>> GetAllAsync(CancellationToken cancellationToken = default);

        Task AddAsync(Member member, CancellationToken cancellationToken = default);

        void Update(Member member);

        void Delete(Member member);
    }
}