using Library.Domain.Entities;

namespace Library.Application.Interfaces;

public interface ITokenService
{
    TokenResult GenerateToken(Member member);
}

public sealed record TokenResult(string Token, DateTime ExpiresAtUtc);
