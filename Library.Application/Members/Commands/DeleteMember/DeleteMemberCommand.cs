using Library.Domain.Shared;
using MediatR;

namespace Library.Application.Members.Commands.DeleteMember
{
    public sealed record DeleteMemberCommand(Guid Id) : IRequest<Result>;
}
