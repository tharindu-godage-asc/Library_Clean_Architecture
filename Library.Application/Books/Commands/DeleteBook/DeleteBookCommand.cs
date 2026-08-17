using Library.Application.Abstractions.Messaging;

namespace Library.Application.Books.Commands.DeleteBook
{
    public sealed record DeleteBookCommand(Guid Id) : ICommand;
}
