using Library.Application.Abstractions.Messaging;

namespace Library.Application.Borrowings.Commands.ReturnBook
{
    public sealed record ReturnBookCommand(Guid BorrowingId) : ICommand;
}
