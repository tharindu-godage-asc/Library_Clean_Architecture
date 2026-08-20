using Library.Application.Abstractions.Messaging;
using Library.Application.Contracts.Auth;

namespace Library.Application.Auth.Commands.Login
{
    public sealed record LoginCommand(
        string Email,
        string Password) : ICommand<LoginResponse>;
}
