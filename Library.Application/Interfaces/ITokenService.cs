using Library.Application.Identity;

namespace Library.Application.Interfaces;

public interface ITokenService
{
    TokenResult GenerateToken(ApplicationUser user, IEnumerable<string> roles, Guid memberId);
}

public sealed record TokenResult(string Token, DateTime ExpiresAtUtc);
