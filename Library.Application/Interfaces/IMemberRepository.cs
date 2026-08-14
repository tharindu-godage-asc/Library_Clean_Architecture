using Library.Domain.Entities;

namespace Library.Application.Interfaces
{
    public interface IMemberRepository
    {
        Task<Member?> GetByIdAsync(Guid id);

        Task<Member?> GetByEmailAsync(string email);

        Task<IEnumerable<Member>> GetAllAsync();

        Task AddAsync(Member member);

        void Update(Member member);

        void Delete(Member member);
    }
}