namespace Library.Application.Interfaces;

public interface ICurrentUserService
{
    Guid? MemberId { get; }

    bool IsAdmin { get; }
}
