using Library.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace Library.Api.Authorization
{
    public class OwnBorrowingRequirement : IAuthorizationRequirement
    {
    }

    public class OwnBorrowingHandler : AuthorizationHandler<OwnBorrowingRequirement, HttpContext>
    {
        private readonly ICurrentUserService _currentUser;
        private readonly IBorrowingRepository _borrowingRepository;

        public OwnBorrowingHandler(
            ICurrentUserService currentUser,
            IBorrowingRepository borrowingRepository)
        {
            _currentUser = currentUser;
            _borrowingRepository = borrowingRepository;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            OwnBorrowingRequirement requirement,
            HttpContext resource)
        {
            if (_currentUser.IsAdmin)
            {
                context.Succeed(requirement);
                return;
            }

            var routeId = resource.GetRouteValue("id") as string;

            if (!Guid.TryParse(routeId, out var borrowingId))
                return;

            var borrowing = await _borrowingRepository.GetByIdAsync(
                borrowingId,
                resource.RequestAborted);

            if (borrowing is not null && borrowing.MemberId == _currentUser.MemberId)
                context.Succeed(requirement);
        }
    }
}
