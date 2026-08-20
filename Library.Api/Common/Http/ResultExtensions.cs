using Library.Domain.Shared;

namespace Library.Api.Common.Http
{
    public static class ResultExtensions
    {
        public static IResult ToProblemDetails(this Result result)
        {
            if (result.IsSuccess)
                throw new InvalidOperationException(
                    "A successful result cannot be converted to a problem response.");

            var statusCode = result.Error.Type switch
            {
                ErrorType.Validation => StatusCodes.Status400BadRequest,
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Conflict => StatusCodes.Status409Conflict,
                ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
                _ => StatusCodes.Status400BadRequest
            };

            return Results.Problem(
                title: result.Error.Code,
                detail: result.Error.Message,
                statusCode: statusCode,
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = result.Error.Code
                });
        }
    }
}
