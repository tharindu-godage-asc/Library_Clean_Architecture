using Library.Api.Authentication;
using Library.Api.Authorization;
using Library.Api.Endpoints;
using Library.Api.Middleware;
using Library.Application;
using Library.Application.Identity;
using Library.Application.Interfaces;
using Library.Infrastructure;
using Library.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Fetch JWT settings
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
var signingKey = builder.Configuration["Jwt:SigningKey"];

if (string.IsNullOrWhiteSpace(signingKey))
{
    throw new InvalidOperationException(
        "Jwt:SigningKey is not configured.");
}

var keycloakAuthority = builder.Configuration["Keycloak:Authority"];
var keycloakAudience = builder.Configuration["Keycloak:Audience"];

// Add Authentication Services
builder.Services
    .AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "Keycloak";
    options.DefaultChallengeScheme = "Keycloak";
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey))
    };
})


.AddJwtBearer("Keycloak", options =>
{
    options.Authority = keycloakAuthority;
    options.RequireHttpsMetadata = false; // local dev only — Keycloak runs over http on localhost
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidAudience = keycloakAudience,
        ValidateLifetime = true,
        // Keycloak stamps a token's "iss" with whatever host:port the client actually used to
        // reach it, not a fixed value — the Android emulator talks to Keycloak via its 10.0.2.2
        // host alias (see KeycloakConfig.issuer in the Flutter app), while this API (and
        // scripts/test-endpoints.ps1) reach the same Keycloak instance via localhost. Without
        // listing both here, tokens minted through the emulator fail issuer validation (401) even
        // though Authority above still resolves signing keys fine, since that request comes from
        // this host, not the emulator.
        ValidIssuers = new[]
        {
            keycloakAuthority,
            keycloakAuthority?.Replace("localhost", "10.0.2.2")
        }
    };
});

builder.Services.AddTransient<IClaimsTransformation, KeycloakRoleClaimsTransformation>();

// Phase 4 cutover: "Keycloak" is now the default authenticate/challenge scheme (above), but every
// policy still explicitly lists both schemes so old, still-valid legacy JWTs keep working
// unchanged — only the default fallback/challenge behavior changed, not what's accepted.
// See docs/keycloak-authserver-phase4-cutover.md.
var authSchemes = new[] { JwtBearerDefaults.AuthenticationScheme, "Keycloak" };

builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder(authSchemes)
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy("AdminOnly", policy =>
        policy.AddAuthenticationSchemes(authSchemes).RequireRole(Roles.Admin));
    options.AddPolicy("OwnMember", policy =>
        policy.AddAuthenticationSchemes(authSchemes).Requirements.Add(new OwnMemberRequirement()));
    options.AddPolicy("OwnBorrowing", policy =>
        policy.AddAuthenticationSchemes(authSchemes).Requirements.Add(new OwnBorrowingRequirement()));
});

builder.Services.AddScoped<IAuthorizationHandler, OwnMemberHandler>();
builder.Services.AddScoped<IAuthorizationHandler, OwnBorrowingHandler>();

// Phase 8 item: every 401 (missing/invalid token) and 403 (AdminOnly role check,
// OwnMember/OwnBorrowing ownership mismatch) previously logged nothing. This is the one place
// that sees every policy's outcome regardless of which handler produced it.
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, LoggingAuthorizationMiddlewareResultHandler>();

// Phase 3 of the Keycloak rollout — JIT Member provisioning.
// See docs/keycloak-authserver-phase3-member-provisioning.md.
builder.Services.AddScoped<IMemberProvisioningService, MemberProvisioningService>();

builder.AddServiceDefaults();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//ASP.NET Core Problem Details and adds a trace ID to every error response. 
//The trace ID helps track a request through logs and diagnostics when troubleshooting issues.ASP.NET 
//Core Problem Details and adds a trace ID to every error response.

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] =
            Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
    };
});

var connectionString = builder.Configuration.GetConnectionString("LibraryDb")!;
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgresql");

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapDefaultEndpoints();

var enableSwagger = app.Configuration.GetValue(
    "EnableSwagger",
    app.Environment.IsDevelopment());

if (enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Skipped in Development: the Flutter client on the Android emulator talks to
// this API over plain HTTP (10.0.2.2:5281) specifically to avoid the
// self-signed dev HTTPS cert being untrusted on-device — a redirect back to
// HTTPS here would just reproduce that same failure. Mirrors how
// KeycloakConfig/AppAuth already treat Keycloak's own HTTP endpoint in dev.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseMiddleware<MemberProvisioningMiddleware>();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    await AdminSeeder.SeedAsync(scope.ServiceProvider);
}

app.MapBookEndpoints();
app.MapMemberEndpoints();
app.MapBorrowingEndpoints();
app.MapAuthEndpoints();

// Originally added as a throwaway Phase 2 verification endpoint (see
// docs/keycloak-authserver-phase2-token-validation.md), but kept deliberately: it's the
// cheapest way to (a) trigger MemberProvisioningMiddleware's JIT provisioning for a Keycloak
// token with no other side effects, and (b) inspect what claims a given Keycloak token actually
// carries. scripts/test-endpoints.ps1's Keycloak coexistence section and manual Keycloak testing
// both rely on it now — no longer temporary.
app.MapGet("/api/keycloak-whoami", (ClaimsPrincipal user) =>
    Results.Ok(user.Claims.Select(c => new { c.Type, c.Value })))
    .RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = "Keycloak" });

app.Run();