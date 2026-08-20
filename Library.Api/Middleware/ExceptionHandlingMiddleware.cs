using FluentValidation;
using Library.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(
            HttpContext context,
            Exception exception)
        {
            _logger.LogError(
                exception,
                "Unhandled exception processing {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            var (statusCode, code, title) = exception switch
            {
                NotFoundException =>
                    (StatusCodes.Status404NotFound, "Resource.NotFound", exception.Message),

                ConflictException =>
                    (StatusCodes.Status409Conflict, "Resource.Conflict", exception.Message),

                BusinessRuleException =>
                    (StatusCodes.Status400BadRequest, "BusinessRule.Violation", exception.Message),

                ValidationException =>
                    (StatusCodes.Status400BadRequest, "Validation.Failed", "Validation failed."),

                _ =>
                    (StatusCodes.Status500InternalServerError, "Unexpected.Error",
                     "An unexpected error occurred.")
            };

            context.Response.StatusCode = statusCode;

            var problemDetailsService =
                context.RequestServices.GetRequiredService<IProblemDetailsService>();

            await problemDetailsService.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails = new ProblemDetails
                {
                    Status = statusCode,
                    Title = title,
                    Extensions =
                    {
                        ["code"] = code
                    }
                }
            });
        }
    }
}