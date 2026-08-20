using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Library.Application;
using Library.Infrastructure;
using Library.Api.Endpoints;
using Library.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//ASP.NET Core Problem Details and adds a trace ID to every error response. 
//The trace ID helps developers track a request through logs and diagnostics when troubleshooting issues.ASP.NET 
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

app.MapHealthChecks("/health");

app.MapHealthChecks("/alive", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live")
});

var enableSwagger = app.Configuration.GetValue(
    "EnableSwagger",
    app.Environment.IsDevelopment());

if (enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapBookEndpoints();
app.MapMemberEndpoints();
app.MapBorrowingEndpoints();

app.Run();