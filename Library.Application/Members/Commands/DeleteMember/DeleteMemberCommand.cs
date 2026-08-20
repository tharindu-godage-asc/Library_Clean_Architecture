using Library.Application.Abstractions.Messaging;

namespace Library.Application.Members.Commands.DeleteMember
{
    public sealed record DeleteMemberCommand(Guid Id) : ICommand;
}
