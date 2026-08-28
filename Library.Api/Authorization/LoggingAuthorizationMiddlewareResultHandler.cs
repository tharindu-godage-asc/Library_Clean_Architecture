using Library.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using System.Security.Claims;

namespace Library.Api.Authorization
{
    /// <summary>
    /// Wraps the framework's default authorization result handler purely to add one Warning-level
    /// log line whenever a request is denied — <see cref="OwnMemberHandler"/>/<see cref="OwnBorrowingHandler"/>
    /// mismatches, the "AdminOnly" role check, and missing/invalid-token challenges on any policy
    /// all flow through here, since none of them log anything on their own. Delegates the actual
    /// response (401 vs 403, WWW-Authenticate header, etc.) to the default handler unchanged.
    /// </summary>
    public class LoggingAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
    {
        private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();
        private readonly ILogger<LoggingAuthorizationMiddlewareResultHandler> _logger;

        public LoggingAuthorizationMiddlewareResultHandler(ILogger<LoggingAuthorizationMiddlewareResultHandler> logger)
        {
            _logger = logger;
        }

        public async Task HandleAsync(
            RequestDelegate next,
            HttpContext context,
            AuthorizationPolicy policy,
            PolicyAuthorizationResult authorizeResult)
        {
            if (!authorizeResult.Succeeded)
            {
                // ICurrentUserService (not a raw claim lookup) so this resolves correctly for both
                // the legacy JWT's "memberId" claim and a Keycloak token's JIT-provisioned stash.
                var currentUser = context.RequestServices.GetRequiredService<ICurrentUserService>();
                var callerId = currentUser.MemberId?.ToString()
                    ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? "anonymous";
                var resourceId = context.GetRouteValue("id") ?? context.GetRouteValue("memberId");
                var statusCode = authorizeResult.Forbidden ? 403 : 401;

                _logger.LogWarning(
                    "Authorization denied: {StatusCode} for caller {CallerId} on {Method} {Path} (resource: {ResourceId})",
                    statusCode, callerId, context.Request.Method, context.Request.Path, resourceId ?? "n/a");
            }

            await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
        }
    }
}
