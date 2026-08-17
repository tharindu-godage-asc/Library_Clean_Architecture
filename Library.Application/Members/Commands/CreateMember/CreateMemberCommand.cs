using Library.Application.Abstractions.Messaging;
using Library.Application.Contracts.Members;

namespace Library.Application.Members.Commands.CreateMember
{
    public sealed record CreateMemberCommand(
        string Name,
        string Email,
        string PhoneNumber) : ICommand<MemberResponse>;
}
