using Library.Application.Abstractions.Messaging;
using Library.Application.Contracts.Auth;
using Library.Application.Identity;
using Library.Application.Interfaces;
using Library.Domain.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Library.Application.Auth.Commands.Login
{
    public sealed class LoginCommandHandler
        : ICommandHandler<LoginCommand, LoginResponse>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly ILogger<LoginCommandHandler> _logger;

        public LoginCommandHandler(
            UserManager<ApplicationUser> userManager,
            ITokenService tokenService,
            ILogger<LoginCommandHandler> logger)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<Result<LoginResponse>> Handle(
            LoginCommand request,
            CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user is null)
            {
                _logger.LogWarning(
                    "Login rejected: no user registered for email {Email}",
                    request.Email);
                return Result.Failure<LoginResponse>(DomainErrors.Auth.InvalidCredentials);
            }

            if (!await _userManager.CheckPasswordAsync(user, request.Password))
            {
                _logger.LogWarning(
                    "Login rejected: incorrect password for user {UserId}",
                    user.Id);
                return Result.Failure<LoginResponse>(DomainErrors.Auth.InvalidCredentials);
            }

            var roles = await _userManager.GetRolesAsync(user);
            var tokenResult = _tokenService.GenerateToken(user, roles, user.MemberId);

            _logger.LogInformation(
                "User {UserId} (member {MemberId}) logged in",
                user.Id,
                user.MemberId);

            return Result.Success(new LoginResponse
            {
                Id = user.MemberId,
                Email = user.Email ?? request.Email,
                Token = tokenResult.Token,
                Expiration = tokenResult.ExpiresAtUtc
            });
        }
    }
}
