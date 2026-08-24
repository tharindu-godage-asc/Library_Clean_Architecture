using Library.Application.Abstractions.Messaging;
using Library.Application.Contracts.Members;

namespace Library.Application.Members.Commands.UpdateMember
{
    public sealed record UpdateMemberCommand(
        Guid Id,
        string Name,
        string Email,
        string PhoneNumber,
        bool? IsActive) : ICommand<MemberResponse>;
}
