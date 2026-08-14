using System;
using Library.Domain.Primitives;

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

        public Book(
            string title,
            string author,
            string isbn,
            int publishedYear,
            int totalCopies)
            : base(Guid.NewGuid())
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Title is required.");

            if (string.IsNullOrWhiteSpace(author))
                throw new ArgumentException("Author is required.");

            if (string.IsNullOrWhiteSpace(isbn))
                throw new ArgumentException("Isbn is required.");

            if (publishedYear > DateTime.UtcNow.Year)
                throw new ArgumentException("Published year cannot be in the future.");

            if (totalCopies <= 0)
                throw new ArgumentException("Total copies must be greater than zero.");

            Title = title;
            Author = author;
            Isbn = isbn;
            PublishedYear = publishedYear;
            TotalCopies = totalCopies; // Fixed: assigned TotalCopies
            AvailableCopies = totalCopies;
        }

        public void BorrowCopy()
        {
            if (AvailableCopies <= 0)
                throw new InvalidOperationException("No available copies.");

            AvailableCopies--;
        }

        public void ReturnCopy()
        {
            if (AvailableCopies >= TotalCopies)
                throw new InvalidOperationException("All copies already accounted for.");

            AvailableCopies++;
        }
    }
}