using System;
using Library.Domain.Primitives;
using Library.Domain.Shared;

namespace Library.Domain.Entities
{
    public class Book : Entity
    {
        public string Title { get; private set; } = default!;

        public string Author { get; private set; } = default!;

        public string Isbn { get; private set; } = default!;

        public int PublishedYear { get; private set; }

        public int TotalCopies { get; private set; }

        public int AvailableCopies { get; private set; }

        private Book() : base(Guid.Empty) { } // Required by EF Core

        private Book(
            string title,
            string author,
            string isbn,
            int publishedYear,
            int totalCopies)
            : base(Guid.NewGuid())
        {
            Title = title;
            Author = author;
            Isbn = isbn;
            PublishedYear = publishedYear;
            TotalCopies = totalCopies;
            AvailableCopies = totalCopies;
        }

        internal static Result<Book> Create(
            string title,
            string author,
            string isbn,
            int publishedYear,
            int totalCopies)
        {
            if (string.IsNullOrWhiteSpace(title))
                return Result.Failure<Book>(DomainErrors.Book.TitleRequired);

            if (string.IsNullOrWhiteSpace(author))
                return Result.Failure<Book>(DomainErrors.Book.AuthorRequired);

            if (string.IsNullOrWhiteSpace(isbn))
                return Result.Failure<Book>(DomainErrors.Book.IsbnRequired);

            if (publishedYear > DateTime.UtcNow.Year)
                return Result.Failure<Book>(DomainErrors.Book.PublishedYearInFuture);

            if (totalCopies <= 0)
                return Result.Failure<Book>(DomainErrors.Book.TotalCopiesInvalid);

            return Result.Success(new Book(title, author, isbn, publishedYear, totalCopies));
        }

        internal Result UpdateDetails(
            string title,
            string author,
            string isbn,
            int publishedYear,
            int totalCopies)
        {
            if (string.IsNullOrWhiteSpace(title))
                return Result.Failure(DomainErrors.Book.TitleRequired);

            if (string.IsNullOrWhiteSpace(author))
                return Result.Failure(DomainErrors.Book.AuthorRequired);

            if (string.IsNullOrWhiteSpace(isbn))
                return Result.Failure(DomainErrors.Book.IsbnRequired);

            if (publishedYear > DateTime.UtcNow.Year)
                return Result.Failure(DomainErrors.Book.PublishedYearInFuture);

            if (totalCopies <= 0)
                return Result.Failure(DomainErrors.Book.TotalCopiesInvalid);

            var borrowedCopies = TotalCopies - AvailableCopies;

            if (totalCopies < borrowedCopies)
                return Result.Failure(DomainErrors.Book.TotalCopiesLessThanBorrowed);

            Title = title;
            Author = author;
            Isbn = isbn;
            PublishedYear = publishedYear;
            TotalCopies = totalCopies;
            AvailableCopies = totalCopies - borrowedCopies;

            return Result.Success();
        }

        internal Result BorrowCopy()
        {
            if (AvailableCopies <= 0)
                return Result.Failure(DomainErrors.Book.NoAvailableCopies);

            AvailableCopies--;
            return Result.Success();
        }

        internal Result ReturnCopy()
        {
            if (AvailableCopies >= TotalCopies)
                return Result.Failure(DomainErrors.Book.AllCopiesAccountedFor);

            AvailableCopies++;
            return Result.Success();
        }
    }
}
