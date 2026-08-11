using Library.Application.Contracts.Books;
using Library.Domain.Entities;

namespace Library.Application.Contracts.Mappings
{
    public static class BookMappings
    {
        public static BookResponse ToResponse(this Book book)
        {
            return new BookResponse
            {
                Id = book.Id,
                Title = book.Title,
                Author = book.Author,
                Isbn = book.Isbn,
                PublishedYear = book.PublishedYear,
                TotalCopies = book.TotalCopies,
                AvailableCopies = book.AvailableCopies
            };
        }
    }
}
