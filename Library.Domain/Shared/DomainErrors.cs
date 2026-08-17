namespace Library.Domain.Shared
{
    public static class DomainErrors
    {
        public static class Email
        {
            public static readonly Error Empty = Error.Validation(
                "Email.Empty",
                "Email is required.");

            public static readonly Error InvalidFormat = Error.Validation(
                "Email.InvalidFormat",
                "Email is not in a valid format.");
        }

        public static class Book
        {
            public static readonly Error TitleRequired = Error.Validation(
                "Book.TitleRequired",
                "Title is required.");

            public static readonly Error AuthorRequired = Error.Validation(
                "Book.AuthorRequired",
                "Author is required.");

            public static readonly Error IsbnRequired = Error.Validation(
                "Book.IsbnRequired",
                "Isbn is required.");

            public static readonly Error PublishedYearInFuture = Error.Validation(
                "Book.PublishedYearInFuture",
                "Published year cannot be in the future.");

            public static readonly Error TotalCopiesInvalid = Error.Validation(
                "Book.TotalCopiesInvalid",
                "Total copies must be greater than zero.");

            public static readonly Error NoAvailableCopies = Error.Failure(
                "Book.NoAvailableCopies",
                "No available copies.");

            public static readonly Error AllCopiesAccountedFor = Error.Failure(
                "Book.AllCopiesAccountedFor",
                "All copies already accounted for.");

            public static readonly Error IsbnAlreadyExists = Error.Conflict(
                "Book.IsbnAlreadyExists",
                "A book with this ISBN already exists.");

            public static readonly Error TotalCopiesLessThanBorrowed = Error.Failure(
                "Book.TotalCopiesLessThanBorrowed",
                "Total copies cannot be less than the number of copies currently borrowed.");

            public static Error NotFound(Guid id) => Error.NotFound(
                "Book.NotFound",
                $"The book with Id = '{id}' was not found.");
        }

        public static class Member
        {
            public static readonly Error NameRequired = Error.Validation(
                "Member.NameRequired",
                "Name is required.");

            public static readonly Error PhoneNumberRequired = Error.Validation(
                "Member.PhoneNumberRequired",
                "Phone number is required.");

            public static readonly Error EmailAlreadyExists = Error.Conflict(
                "Member.EmailAlreadyExists",
                "A member with this email already exists.");

            public static readonly Error Inactive = Error.Failure(
                "Member.Inactive",
                "Member is inactive.");

            public static Error NotFound(Guid id) => Error.NotFound(
                "Member.NotFound",
                $"The member with Id = '{id}' was not found.");
        }

        public static class Borrowing
        {
            public static readonly Error InvalidDueDate = Error.Validation(
                "Borrowing.InvalidDueDate",
                "Due date must be after borrow date.");

            public static readonly Error AlreadyReturned = Error.Failure(
                "Borrowing.AlreadyReturned",
                "Book has already been returned.");

            public static readonly Error LimitExceeded = Error.Failure(
                "Borrowing.LimitExceeded",
                "Member borrowing limit exceeded.");

            public static Error NotFound(Guid id) => Error.NotFound(
                "Borrowing.NotFound",
                $"The borrowing record with Id = '{id}' was not found.");
        }
    }
}
