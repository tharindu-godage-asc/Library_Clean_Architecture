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

        public async Task<Borrowing?> GetByIdAsync(Guid id)
        {
            return await _context.Borrowings
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<IEnumerable<Borrowing>> GetAllAsync()
        {
            return await _context.Borrowings.ToListAsync();
        }

        public async Task<IEnumerable<Borrowing>> GetActiveBorrowingsAsync()
        {
            return await _context.Borrowings
                .Where(b => b.Status == BorrowingStatus.Active)
                .ToListAsync();
        }

        public async Task<int> CountActiveForMemberAsync(Guid memberId)
        {
            return await _context.Borrowings.CountAsync(
                b => b.MemberId == memberId &&
                     b.Status == BorrowingStatus.Active);
        }

        public async Task AddAsync(Borrowing borrowing)
        {
            await _context.Borrowings.AddAsync(borrowing);
        }

        public void Update(Borrowing borrowing)
        {
            _context.Borrowings.Update(borrowing);
        }
    }
}