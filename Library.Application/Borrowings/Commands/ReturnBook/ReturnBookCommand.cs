using Library.Domain.Shared;
using MediatR;

namespace Library.Application.Borrowings.Commands.ReturnBook
{
    public sealed record ReturnBookCommand(Guid BorrowingId) : IRequest<Result>;
}
