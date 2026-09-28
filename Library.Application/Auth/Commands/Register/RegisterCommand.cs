using Library.Application.Abstractions.Messaging;
using Library.Application.Contracts.Members;

namespace Library.Application.Auth.Commands.Register
{
    [Obsolete("Superseded by Keycloak-hosted registration + JIT Member provisioning; see docs/keycloak-authserver-phase4-cutover.md")]
    public sealed record RegisterCommand(
        string Name,
        string Email,
        string PhoneNumber,
        string Password) : ICommand<MemberResponse>;
}
