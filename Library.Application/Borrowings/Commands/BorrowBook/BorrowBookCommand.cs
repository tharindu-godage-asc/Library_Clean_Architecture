using Library.Application.Contracts.Borrowings;
using Library.Domain.Shared;
using MediatR;

namespace Library.Application.Borrowings.Commands.BorrowBook
{
    public sealed record BorrowBookCommand(
        Guid MemberId,
        Guid BookId) : IRequest<Result<BorrowingResponse>>;
}
