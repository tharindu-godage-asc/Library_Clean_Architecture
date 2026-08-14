using Library.Domain.Exceptions;
using Library.Domain.Shared;

namespace Library.Application.Common
{
    public static class ResultExtensions
    {
        public static T GetValueOrThrow<T>(this Result<T> result)
        {
            if (result.IsFailure)
                throw new BusinessRuleException(result.Error.Message);

            return result.Value;
        }

        public static void ThrowIfFailure(this Result result)
        {
            if (result.IsFailure)
                throw new BusinessRuleException(result.Error.Message);
        }
    }
}
