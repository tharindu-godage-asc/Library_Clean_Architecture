using Library.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace Library.Api.Authorization
{
    public class OwnMemberRequirement : IAuthorizationRequirement
    {
    }

    public class OwnMemberHandler : AuthorizationHandler<OwnMemberRequirement, HttpContext>
    {
        private readonly ICurrentUserService _currentUser;

        public OwnMemberHandler(ICurrentUserService currentUser)
        {
            _currentUser = currentUser;
        }

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            OwnMemberRequirement requirement,
            HttpContext resource)
        {
            if (_currentUser.IsAdmin)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            var routeId = resource.GetRouteValue("id") as string;

            if (Guid.TryParse(routeId, out var memberId) && memberId == _currentUser.MemberId)
                context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }
}
