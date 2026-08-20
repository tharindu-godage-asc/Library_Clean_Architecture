using Library.Application.Interfaces;
using Library.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Library.Infrastructure.Security
{
    public class PasswordHasher : IPasswordHasher
    {
        private readonly Microsoft.AspNetCore.Identity.PasswordHasher<Member> _hasher = new();

        public string Hash(Member member, string password)
        {
            return _hasher.HashPassword(member, password);
        }

        public bool Verify(Member member, string hashedPassword, string providedPassword)
        {
            var result = _hasher.VerifyHashedPassword(member, hashedPassword, providedPassword);

            return result != PasswordVerificationResult.Failed;
        }
    }
}
