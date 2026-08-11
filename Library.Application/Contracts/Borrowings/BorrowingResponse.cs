using System;
using System.Collections.Generic;
using System.Text;

using Library.Domain.Enums;

namespace Library.Application.Contracts.Borrowings
{
    public class BorrowingResponse
    {
        public int Id { get; set; }

        public int BookId { get; set; }

        public int MemberId { get; set; }

        public DateTime BorrowedAt { get; set; }

        public DateTime DueDate { get; set; }

        public DateTime? ReturnedAt { get; set; }

        public BorrowingStatus Status { get; set; }
    }
}
