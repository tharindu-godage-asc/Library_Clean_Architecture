using System.Text.RegularExpressions;
using Library.Domain.Primitives;
using Library.Domain.Shared;

namespace Library.Domain.ValueObjects
{
    public sealed class Email : ValueObject
    {
        private static readonly Regex Format = new(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.Compiled);

        public string Value { get; }

        private Email(string value)
        {
            Value = value;
        }

        public static Result<Email> Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Result.Failure<Email>(DomainErrors.Email.Empty);

            var normalized = value.Trim().ToLowerInvariant();

            if (!Format.IsMatch(normalized))
                return Result.Failure<Email>(DomainErrors.Email.InvalidFormat);

            return Result.Success(new Email(normalized));
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }

        public override string ToString() => Value;
    }
}
