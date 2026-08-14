using Library.Domain.Enums;
using Library.Domain.Primitives;

namespace Library.Domain.Entities;

public class Borrowing : Entity
{
    public Guid BookId { get; private set; }

    public Guid MemberId { get; private set; }

    public DateTime BorrowedAt { get; private set; }

    public DateTime DueDate { get; private set; }

    public DateTime? ReturnedAt { get; private set; }

    public BorrowingStatus Status { get; private set; }

    private Borrowing() : base(Guid.Empty)
    {
    }

    public Borrowing(
        Guid bookId,
        Guid memberId,
        DateTime borrowedAt,
        DateTime dueDate)
        : base(Guid.NewGuid())
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