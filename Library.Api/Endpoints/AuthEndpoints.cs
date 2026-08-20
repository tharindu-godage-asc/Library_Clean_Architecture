namespace Library.Api.Endpoints
{
    public static class AuthEndpoints
    {
        public static IEndpointRouteBuilder MapAuthEndpoints(
            this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/auth")
                .WithTags("Auth");

            // Register/login are being rebuilt against ASP.NET Core Identity's
            // UserManager/RoleManager (Phase B) — temporarily empty during the migration.

            return app;
        }
    }
}
