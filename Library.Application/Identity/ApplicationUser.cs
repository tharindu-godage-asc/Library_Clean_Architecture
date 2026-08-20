using Microsoft.AspNetCore.Identity;

namespace Library.Application.Identity
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public Guid MemberId { get; set; }
    }
}
