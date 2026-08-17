using Library.Application.Contracts.Members;
using Library.Domain.Shared;
using MediatR;

namespace Library.Application.Members.Commands.UpdateMember
{
    public sealed record UpdateMemberCommand(
        Guid Id,
        string Name,
        string Email,
        string PhoneNumber) : IRequest<Result<MemberResponse>>;
}
