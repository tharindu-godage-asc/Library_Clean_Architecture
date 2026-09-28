using Library.Application.Identity;
using Library.Application.Interfaces;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace Library.Api.Middleware;

public class MemberProvisioningMiddleware
{
    private readonly RequestDelegate _next;

    public MemberProvisioningMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IMemberProvisioningService provisioningService)
    {
        var authResult = await context.AuthenticateAsync("Keycloak");

        if (authResult.Succeeded && authResult.Principal is not null)
        {
            var keycloakId = authResult.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = authResult.Principal.FindFirst(ClaimTypes.Email)?.Value;
            var name = authResult.Principal.FindFirst("name")?.Value;

            if (!string.IsNullOrWhiteSpace(keycloakId) && !string.IsNullOrWhiteSpace(email))
            {
                var memberId = await provisioningService.EnsureMemberAsync(
                    keycloakId,
                    email,
                    name,
                    context.RequestAborted);

                context.Items[HttpContextItemKeys.KeycloakMemberId] = memberId;
            }
        }

        await _next(context);
    }
}
