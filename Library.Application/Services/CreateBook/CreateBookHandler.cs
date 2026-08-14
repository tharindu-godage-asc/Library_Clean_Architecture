using Library.Application.Books.CreateBook;
using Library.Application.Contracts.Books;
using Library.Application.Contracts.Mappings;
using Library.Application.Interfaces;
using Library.Domain.Entities;
using Library.Domain.Exceptions;

public class CreateBookHandler
{
    private readonly IBookRepository _bookRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateBookHandler(
        IBookRepository bookRepository,
        IUnitOfWork unitOfWork)
    {
        _bookRepository = bookRepository;
        _unitOfWork = unitOfWork;
    }


    //Creating of a new Book
    public async Task<BookResponse> Handle(CreateBookCommand command)
    {
        var existingBook =
            await _bookRepository.GetByIsbnAsync(command.Isbn);

        if (existingBook is not null)
            throw new ConflictException(
                "A book with this ISBN already exists.");

        var book = new Book(
            command.Title,
            command.Author,
            command.Isbn,
            command.PublishedYear,
            command.TotalCopies);

        await _bookRepository.AddAsync(book);

        await _unitOfWork.SaveChangesAsync();

        return book.ToResponse();
    }
}
