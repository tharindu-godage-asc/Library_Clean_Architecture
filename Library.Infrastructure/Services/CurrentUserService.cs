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
                var context = _httpContextAccessor.HttpContext;

                // Keycloak-authenticated requests: MemberProvisioningMiddleware already
                // resolved (or JIT-created) the Member and stashed its id here — Keycloak
                // tokens carry no "memberId" claim of their own.
                if (context?.Items.TryGetValue(HttpContextItemKeys.KeycloakMemberId, out var stashed) == true
                    && stashed is Guid stashedMemberId)
                {
                    return stashedMemberId;
                }

                var value = context?.User.FindFirst("memberId")?.Value;
                return Guid.TryParse(value, out var memberId) ? memberId : null;
            }
        }

        public bool IsAdmin =>
            _httpContextAccessor.HttpContext?.User.IsInRole(Roles.Admin) ?? false;
    }
}
