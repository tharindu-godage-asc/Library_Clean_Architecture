using System;
using System.Collections.Generic;
using System.Text;

namespace Library.Application.Contracts.Borrowings
{
    public class CreateBorrowingRequest
    {
        public int BookId { get; set; }

        public int MemberId { get; set; }
    }
}
