using Library.Application.Contracts.Members;
using Library.Domain.Shared;
using MediatR;

namespace Library.Application.Members.Commands.CreateMember
{
    public sealed record CreateMemberCommand(
        string Name,
        string Email,
        string PhoneNumber) : IRequest<Result<MemberResponse>>;
}
