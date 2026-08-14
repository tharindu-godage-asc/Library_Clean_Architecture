using System.Text.RegularExpressions;
using Library.Domain.Primitives;

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

        public static Email Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Email is required.");

            var normalized = value.Trim().ToLowerInvariant();

            if (!Format.IsMatch(normalized))
                throw new ArgumentException("Email is not in a valid format.");

            return new Email(normalized);
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }

        public override string ToString() => Value;
    }
}
