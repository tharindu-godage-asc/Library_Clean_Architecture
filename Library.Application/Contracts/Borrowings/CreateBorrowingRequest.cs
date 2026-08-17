using System;
using System.Collections.Generic;
using System.Text;

namespace Library.Application.Contracts.Borrowings
{
    public class CreateBorrowingRequest
    {
        public Guid BookId { get; set; }

        public Guid MemberId { get; set; }
    }
}
