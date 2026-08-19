using FluentValidation;
using Library.Domain.Exceptions;
using System.Text.Json;

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

            var (statusCode, message) = exception switch
            {
                NotFoundException =>
                    (StatusCodes.Status404NotFound, exception.Message),

                ConflictException =>
                    (StatusCodes.Status409Conflict, exception.Message),

                BusinessRuleException =>
                    (StatusCodes.Status400BadRequest, exception.Message),

                ValidationException =>
                    (StatusCodes.Status400BadRequest, "Validation failed."),

                _ =>
                    (StatusCodes.Status500InternalServerError,
                     "An unexpected error occurred.")
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            var response = new
            {
                StatusCode = statusCode,
                Message = message
            };

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(response));
        }
    }
}