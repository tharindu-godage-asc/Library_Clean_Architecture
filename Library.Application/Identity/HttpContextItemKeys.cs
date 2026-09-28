namespace Library.Application.Identity;

/// <summary>
/// HttpContext.Items keys shared between Library.Api's MemberProvisioningMiddleware (which sets
/// them) and Library.Infrastructure's CurrentUserService (which reads them) — kept here, not in
/// either project directly, since Infrastructure must not depend on Api.
/// </summary>
public static class HttpContextItemKeys
{
    public const string KeycloakMemberId = "Library.Keycloak.MemberId";
}
