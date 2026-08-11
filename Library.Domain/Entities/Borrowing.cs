using Library.Domain.Enums;

namespace Library.Domain.Entities;

public class Borrowing
{
    public int Id { get; private set; }

    public int BookId { get; private set; }

    public int MemberId { get; private set; }

    public DateTime BorrowedAt { get; private set; }

    public DateTime DueDate { get; private set; }

    public DateTime? ReturnedAt { get; private set; }

    public BorrowingStatus Status { get; private set; }

    private Borrowing()
    {
    }

    public Borrowing(
        int bookId,
        int memberId,
        DateTime borrowedAt,
        DateTime dueDate)
    {
        if (dueDate <= borrowedAt)
            throw new ArgumentException("Due date must be after borrow date.");

        BookId = bookId;
        MemberId = memberId;
        BorrowedAt = borrowedAt;
        DueDate = dueDate;
        Status = BorrowingStatus.Active;
    }

    public void ReturnBook()
    {
        if (Status == BorrowingStatus.Returned)
            throw new InvalidOperationException("Book has already been returned.");

        ReturnedAt = DateTime.UtcNow;
        Status = BorrowingStatus.Returned;
    }
}