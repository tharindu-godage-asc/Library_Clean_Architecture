using Library.Application.Abstractions.Messaging;
using Library.Application.Contracts.Auth;
using Library.Application.Interfaces;
using Library.Domain.Shared;
using Microsoft.Extensions.Logging;

namespace Library.Application.Auth.Commands.Login
{
    public sealed class LoginCommandHandler
        : ICommandHandler<LoginCommand, LoginResponse>
    {
        private readonly IMemberRepository _memberRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<LoginCommandHandler> _logger;

        public LoginCommandHandler(
            IMemberRepository memberRepository,
            IPasswordHasher passwordHasher,
            ILogger<LoginCommandHandler> logger)
        {
            _memberRepository = memberRepository;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        public async Task<Result<LoginResponse>> Handle(
            LoginCommand request,
            CancellationToken cancellationToken)
        {
            var member = await _memberRepository.GetByEmailAsync(request.Email, cancellationToken);

            if (member is null)
            {
                _logger.LogWarning(
                    "Login rejected: no member registered for email {Email}",
                    request.Email);
                return Result.Failure<LoginResponse>(DomainErrors.Auth.InvalidCredentials);
            }

            if (!_passwordHasher.Verify(member, member.PasswordHash, request.Password))
            {
                _logger.LogWarning(
                    "Login rejected: incorrect password for member {MemberId}",
                    member.Id);
                return Result.Failure<LoginResponse>(DomainErrors.Auth.InvalidCredentials);
            }

            _logger.LogInformation(
                "Member {MemberId} logged in",
                member.Id);

            return Result.Success(new LoginResponse
            {
                Id = member.Id,
                Email = member.Email.Value
            });
        }
    }
}
