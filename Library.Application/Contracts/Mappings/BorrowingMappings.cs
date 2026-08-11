using Library.Application.Contracts.Borrowings;
using Library.Domain.Entities;

namespace Library.Application.Contracts.Mappings
{
    public static class BorrowingMappings
    {
        public static BorrowingResponse ToResponse(this Borrowing borrowing)
        {
            return new BorrowingResponse
            {
                Id = borrowing.Id,
                BookId = borrowing.BookId,
                MemberId = borrowing.MemberId,
                BorrowedAt = borrowing.BorrowedAt,
                DueDate = borrowing.DueDate,
                ReturnedAt = borrowing.ReturnedAt,
                Status = borrowing.Status
            };
        }
    }
}