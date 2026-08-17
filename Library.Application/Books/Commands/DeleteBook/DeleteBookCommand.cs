using Library.Domain.Shared;
using MediatR;

namespace Library.Application.Books.Commands.DeleteBook
{
    public sealed record DeleteBookCommand(Guid Id) : IRequest<Result>;
}
