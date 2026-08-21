using Library.Application.Identity;
using Library.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Library.Infrastructure.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid? MemberId
        {
            get
            {
                var value = _httpContextAccessor.HttpContext?.User.FindFirst("memberId")?.Value;
                return Guid.TryParse(value, out var memberId) ? memberId : null;
            }
        }

        public bool IsAdmin =>
            _httpContextAccessor.HttpContext?.User.IsInRole(Roles.Admin) ?? false;
    }
}
