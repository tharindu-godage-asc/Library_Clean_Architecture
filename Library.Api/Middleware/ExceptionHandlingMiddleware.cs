using FluentValidation;
using Library.Domain.Exceptions;
using System.Text.Json;

namespace Library.Api.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
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

        private static async Task HandleExceptionAsync(
            HttpContext context,
            Exception exception)
        {
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