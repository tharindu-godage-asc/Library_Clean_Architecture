using Library.Domain.Entities;

namespace Library.Application.Interfaces
{
    public interface IPasswordHasher
    {
        string Hash(Member member, string password);

        bool Verify(Member member, string hashedPassword, string providedPassword);
    }
}
