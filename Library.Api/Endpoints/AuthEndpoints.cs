using Library.Api.Common.Filters;
using Library.Api.Common.Http;
using Library.Application.Auth.Commands.Login;
using Library.Application.Auth.Commands.Register;
using Library.Application.Contracts.Auth;
using MediatR;

namespace Library.Api.Endpoints
{
    public static class AuthEndpoints
    {
        public static IEndpointRouteBuilder MapAuthEndpoints(
            this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/auth")
                .WithTags("Auth");

            // Deprecated (Phase 4 cutover) — Keycloak's hosted page now handles registration,
            // and JIT Member provisioning creates the Member record. Kept, not deleted, per
            // standing preference. See docs/keycloak-authserver-phase4-cutover.md.
            // (RegisterCommand/Handler are [Obsolete] — the resulting build warnings here are
            // intentional, documenting this call site as legacy.)
            group.MapPost("/register", async (
                RegisterRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new RegisterCommand(
                    request.Name,
                    request.Email,
                    request.PhoneNumber,
                    request.Password);

                var result = await sender.Send(command, cancellationToken);

                return result.IsSuccess
                    ? Results.Created($"/api/members/{result.Value.Id}", result.Value)
                    : result.ToProblemDetails();
            })
            .AddEndpointFilter<ValidationFilter<RegisterRequest>>()
            .WithDescription("Deprecated — superseded by Keycloak-hosted registration.");

            // Deprecated (Phase 4 cutover) — superseded by Keycloak-issued tokens. Kept, not
            // deleted, per standing preference. See docs/keycloak-authserver-phase4-cutover.md.
            group.MapPost("/login", async (
                LoginRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new LoginCommand(
                    request.Email,
                    request.Password);

                var result = await sender.Send(command, cancellationToken);

                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.ToProblemDetails();
            })
            .AddEndpointFilter<ValidationFilter<LoginRequest>>()
            .WithDescription("Deprecated — superseded by Keycloak-issued tokens.");

            return app;
        }
    }
}
