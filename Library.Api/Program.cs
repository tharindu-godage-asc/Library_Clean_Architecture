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

// Fetch Keycloak settings (Phase 2 of the OAuth Authorization Server rollout —
// see docs/keycloak-authserver-phase2-token-validation.md)
var keycloakAuthority = builder.Configuration["Keycloak:Authority"];
var keycloakAudience = builder.Configuration["Keycloak:Audience"];

// Add Authentication Services
builder.Services
    .AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
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
// Second, additional scheme — validates Keycloak-issued tokens. Not the default yet
// (that switch happens in Phase 4); the original "Bearer" scheme above is untouched.
.AddJwtBearer("Keycloak", options =>
{
    options.Authority = keycloakAuthority;
    options.RequireHttpsMetadata = false; // local dev only — Keycloak runs over http on localhost
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidAudience = keycloakAudience,
        ValidateLifetime = true
        // IssuerSigningKey intentionally not set: Authority above makes the handler fetch
        // signing keys from Keycloak's JWKS endpoint automatically.
    };
});

builder.Services.AddTransient<IClaimsTransformation, KeycloakRoleClaimsTransformation>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(Roles.Admin));
    options.AddPolicy("OwnMember", policy => policy.Requirements.Add(new OwnMemberRequirement()));
    options.AddPolicy("OwnBorrowing", policy => policy.Requirements.Add(new OwnBorrowingRequirement()));
});

builder.Services.AddScoped<IAuthorizationHandler, OwnMemberHandler>();
builder.Services.AddScoped<IAuthorizationHandler, OwnBorrowingHandler>();

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

app.UseHttpsRedirection();

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

// Temporary — Phase 2 verification only, removed once Phase 3's real flow is proven.
// See docs/keycloak-authserver-phase2-token-validation.md.
app.MapGet("/api/keycloak-whoami", (ClaimsPrincipal user) =>
    Results.Ok(user.Claims.Select(c => new { c.Type, c.Value })))
    .RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = "Keycloak" });

app.Run();