namespace Library.Application.Contracts.Auth
{
    public class LoginResponse
    {
        public Guid Id { get; set; }

        public string Email { get; set; } = string.Empty;
    }
}
