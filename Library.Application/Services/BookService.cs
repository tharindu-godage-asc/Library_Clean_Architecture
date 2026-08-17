using Library.Application.Interfaces;
using Library.Domain.Entities;
using Library.Domain.Exceptions;

namespace Library.Application.Services
{
    public class BookService
    {
        private readonly IBookRepository _bookRepository;
        private readonly IUnitOfWork _unitOfWork;

        public BookService(
            IBookRepository bookRepository,
            IUnitOfWork unitOfWork)
        {
            _bookRepository = bookRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<Book>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _bookRepository.GetAllAsync(cancellationToken);
        }

        public async Task<Book> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _bookRepository.GetByIdAsync(id, cancellationToken)
                   ?? throw new NotFoundException("Book not found.");
        }

        public async Task CreateAsync(Book book, CancellationToken cancellationToken = default)
        {
            var existingBook = await _bookRepository.GetByIsbnAsync(book.Isbn, cancellationToken);

            if (existingBook is not null)
                throw new ConflictException("A book with this ISBN already exists.");

            await _bookRepository.AddAsync(book, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var book = await _bookRepository.GetByIdAsync(id, cancellationToken)
                       ?? throw new NotFoundException("Book not found.");

            _bookRepository.Delete(book);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}