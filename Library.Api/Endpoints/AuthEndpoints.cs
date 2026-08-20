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
            .AddEndpointFilter<ValidationFilter<RegisterRequest>>();

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
            .AddEndpointFilter<ValidationFilter<LoginRequest>>();

            return app;
        }
    }
}
