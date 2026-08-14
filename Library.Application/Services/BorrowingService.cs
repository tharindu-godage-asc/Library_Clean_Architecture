using Library.Application.Interfaces;
using Library.Domain.Entities;
using Library.Domain.Exceptions;

namespace Library.Application.Services
{
    public class BorrowingService
    {
        private readonly IBookRepository _bookRepository;
        private readonly IMemberRepository _memberRepository;
        private readonly IBorrowingRepository _borrowingRepository;
        private readonly IUnitOfWork _unitOfWork;

        public BorrowingService(
            IBookRepository bookRepository,
            IMemberRepository memberRepository,
            IBorrowingRepository borrowingRepository,
            IUnitOfWork unitOfWork)
        {
            _bookRepository = bookRepository;
            _memberRepository = memberRepository;
            _borrowingRepository = borrowingRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Borrowing> BorrowBookAsync(
            Guid memberId,
            Guid bookId)
        {
            var member = await _memberRepository.GetByIdAsync(memberId)
                ?? throw new NotFoundException("Member not found.");

            var book = await _bookRepository.GetByIdAsync(bookId)
                ?? throw new NotFoundException("Book not found.");

            if (!member.IsActive)
                throw new BusinessRuleException("Member is inactive.");

            var activeBorrowings =
                await _borrowingRepository.CountActiveForMemberAsync(memberId);

            if (activeBorrowings >= 3)
                throw new BusinessRuleException(
                    "Member borrowing limit exceeded.");

            book.BorrowCopy();

            var borrowing = new Borrowing(
                bookId,
                memberId,
                DateTime.UtcNow,
                DateTime.UtcNow.AddDays(14));

            await _borrowingRepository.AddAsync(borrowing);

            _bookRepository.Update(book);

            await _unitOfWork.SaveChangesAsync();

            return borrowing;
        }

        public async Task ReturnBookAsync(Guid borrowingId)
        {
            var borrowing =
                await _borrowingRepository.GetByIdAsync(borrowingId)
                ?? throw new NotFoundException("Borrowing record not found.");

            var book =
                await _bookRepository.GetByIdAsync(borrowing.BookId)
                ?? throw new NotFoundException("Book not found.");

            borrowing.ReturnBook();

            book.ReturnCopy();

            _borrowingRepository.Update(borrowing);

            _bookRepository.Update(book);

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<IEnumerable<Borrowing>> GetAllAsync()
        {
            return await _borrowingRepository.GetAllAsync();
        }

        public async Task<Borrowing> GetByIdAsync(Guid id)
        {
            return await _borrowingRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Borrowing record not found.");
        }
    }
}