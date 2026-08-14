using Library.Domain.Enums;
using Library.Domain.Primitives;
using Library.Domain.Shared;

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

    private Borrowing(
        Guid bookId,
        Guid memberId,
        DateTime borrowedAt,
        DateTime dueDate)
        : base(Guid.NewGuid())
    {
        BookId = bookId;
        MemberId = memberId;
        BorrowedAt = borrowedAt;
        DueDate = dueDate;
        Status = BorrowingStatus.Active;
    }

    public static Result<Borrowing> Create(
        Guid bookId,
        Guid memberId,
        DateTime borrowedAt,
        DateTime dueDate)
    {
        if (dueDate <= borrowedAt)
            return Result.Failure<Borrowing>(DomainErrors.Borrowing.InvalidDueDate);

        return Result.Success(new Borrowing(bookId, memberId, borrowedAt, dueDate));
    }

    public Result ReturnBook()
    {
        if (Status == BorrowingStatus.Returned)
            return Result.Failure(DomainErrors.Borrowing.AlreadyReturned);

        ReturnedAt = DateTime.UtcNow;
        Status = BorrowingStatus.Returned;
        return Result.Success();
    }
}
