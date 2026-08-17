using System;
using System.Collections.Generic;
using System.Text;

using Library.Domain.Enums;

namespace Library.Application.Contracts.Borrowings
{
    public class BorrowingResponse
    {
        public Guid Id { get; set; }

        public Guid BookId { get; set; }

        public Guid MemberId { get; set; }

        public DateTime BorrowedAt { get; set; }

        public DateTime DueDate { get; set; }

        public DateTime? ReturnedAt { get; set; }

        public BorrowingStatus Status { get; set; }
    }
}
