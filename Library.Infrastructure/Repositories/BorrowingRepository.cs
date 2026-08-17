using Library.Application.Interfaces;
using Library.Domain.Entities;
using Library.Domain.Enums;
using Library.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Repositories
{
    public class BorrowingRepository : IBorrowingRepository
    {
        private readonly LibraryDbContext _context;

        public BorrowingRepository(LibraryDbContext context)
        {
            _context = context;
        }

        public async Task<Borrowing?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Borrowings
                .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<Borrowing>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Borrowings.ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Borrowing>> GetActiveBorrowingsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Borrowings
                .Where(b => b.Status == BorrowingStatus.Active)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Borrowing>> GetByMemberIdAsync(Guid memberId, CancellationToken cancellationToken = default)
        {
            return await _context.Borrowings
                .Where(b => b.MemberId == memberId)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> CountActiveForMemberAsync(Guid memberId, CancellationToken cancellationToken = default)
        {
            return await _context.Borrowings.CountAsync(
                b => b.MemberId == memberId &&
                     b.Status == BorrowingStatus.Active,
                cancellationToken);
        }

        public async Task AddAsync(Borrowing borrowing, CancellationToken cancellationToken = default)
        {
            await _context.Borrowings.AddAsync(borrowing, cancellationToken);
        }

        public void Update(Borrowing borrowing)
        {
            _context.Borrowings.Update(borrowing);
        }
    }
}