using FluentValidation;

namespace Library.Api.Common.Filters
{
    public class ValidationFilter<T> : IEndpointFilter
    {
        private readonly IValidator<T> _validator;

        public ValidationFilter(IValidator<T> validator)
        {
            _validator = validator;
        }

        public async ValueTask<object?> InvokeAsync(
            EndpointFilterInvocationContext context,
            EndpointFilterDelegate next)
        {
            var argument = context.Arguments
                .OfType<T>()
                .FirstOrDefault();

            if (argument is null)
            {
                return await next(context);
            }

            var validationResult =
                await _validator.ValidateAsync(argument);

            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(
                    validationResult.ToDictionary());
            }

            return await next(context);
        }
    }
}