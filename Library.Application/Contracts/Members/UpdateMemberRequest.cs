namespace Library.Application.Contracts.Members
{
    public class UpdateMemberRequest
    {
        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public bool? IsActive { get; set; }
    }
}
