using Library.Application.Interfaces;
using Library.Domain.Entities;
using Library.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Repositories
{
    public class MemberRepository : IMemberRepository
    {
        private readonly LibraryDbContext _context;

        public MemberRepository(LibraryDbContext context)
        {
            _context = context;
        }

        public async Task<Member?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Members
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        }

        public async Task<Member?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return await _context.Members
                .FirstOrDefaultAsync(m => m.Email.Value == email, cancellationToken);
        }

        public async Task<Member?> GetByKeycloakIdAsync(string keycloakId, CancellationToken cancellationToken = default)
        {
            return await _context.Members
                .FirstOrDefaultAsync(m => m.KeycloakId == keycloakId, cancellationToken);
        }

        public async Task<IEnumerable<Member>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Members.ToListAsync(cancellationToken);
        }

        public async Task AddAsync(Member member, CancellationToken cancellationToken = default)
        {
            await _context.Members.AddAsync(member, cancellationToken);
        }

        public void Update(Member member)
        {
            _context.Members.Update(member);
        }

        public void Delete(Member member)
        {
            _context.Members.Remove(member);
        }
    }
}