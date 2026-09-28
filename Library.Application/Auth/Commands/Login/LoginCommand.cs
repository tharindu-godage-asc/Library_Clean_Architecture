using Library.Application.Abstractions.Messaging;
using Library.Application.Contracts.Auth;

namespace Library.Application.Auth.Commands.Login
{
    [Obsolete("Superseded by Keycloak-issued tokens; see docs/keycloak-authserver-phase4-cutover.md")]
    public sealed record LoginCommand(
        string Email,
        string Password) : ICommand<LoginResponse>;
}
