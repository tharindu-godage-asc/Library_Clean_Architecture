using Library.Application.Abstractions.Messaging;
using Library.Application.Contracts.Members;

namespace Library.Application.Auth.Commands.Register
{
    public sealed record RegisterCommand(
        string Name,
        string Email,
        string PhoneNumber,
        string Password) : ICommand<MemberResponse>;
}
