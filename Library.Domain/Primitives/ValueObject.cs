namespace Library.Domain.Primitives
{
    public abstract class ValueObject : IEquatable<ValueObject>
    {
        protected abstract IEnumerable<object?> GetEqualityComponents();

        public bool Equals(ValueObject? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (other.GetType() != GetType()) return false;
            return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as ValueObject);
        }

        public override int GetHashCode()
        {
            return GetEqualityComponents()
                .Select(component => component?.GetHashCode() ?? 0)
                .Aggregate((first, second) => first ^ second);
        }
    }
}
