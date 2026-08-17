using Library.Application.Abstractions.Messaging;
using Library.Application.Contracts.Borrowings;

namespace Library.Application.Borrowings.Commands.BorrowBook
{
    public sealed record BorrowBookCommand(
        Guid MemberId,
        Guid BookId) : ICommand<BorrowingResponse>;
}
