using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace Library.Api.Authentication;

/// <summary>
/// Keycloak puts realm roles in a "realm_access": { "roles": [...] } claim, not as
/// individual ClaimTypes.Role claims. This bridges that shape into standard role claims so
/// RequireRole/[Authorize(Roles=...)] (AdminOnly, etc.) keep working unchanged.
/// </summary>
public class KeycloakRoleClaimsTransformation : IClaimsTransformation
{
    private const string RealmAccessClaimType = "realm_access";

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = principal.Identity as ClaimsIdentity;
        if (identity is null || !identity.IsAuthenticated)
        {
            return Task.FromResult(principal);
        }

        var realmAccessJson = identity.FindFirst(RealmAccessClaimType)?.Value;
        if (string.IsNullOrWhiteSpace(realmAccessJson))
        {
            return Task.FromResult(principal);
        }

        using var document = JsonDocument.Parse(realmAccessJson);
        if (!document.RootElement.TryGetProperty("roles", out var rolesElement))
        {
            return Task.FromResult(principal);
        }

        foreach (var role in rolesElement.EnumerateArray())
        {
            var roleName = role.GetString();
            if (!string.IsNullOrWhiteSpace(roleName) && !identity.HasClaim(ClaimTypes.Role, roleName))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
            }
        }

        return Task.FromResult(principal);
    }
}
