namespace Library.Application.Interfaces;

/// <summary>
/// Just-in-time provisioning of a Member from a Keycloak-authenticated identity.
/// See Library.Application.Identity.MemberProvisioningService for the three-step
/// lookup (KeycloakId -> Email -> create) and why the Email fallback matters.
/// </summary>
public interface IMemberProvisioningService
{
    Task<Guid> EnsureMemberAsync(
        string keycloakId,
        string email,
        string? name,
        CancellationToken cancellationToken = default);
}
