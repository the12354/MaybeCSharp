// ReSharper disable CompareNonConstrainedGenericWithNull
namespace MaybeCSharp
{
    public class Maybe<T>
    {
        public T Value { get; private set; }
        public bool HasValue { get; private set; }

        public Maybe(T value)
        {
            Value = value;
            HasValue = (value != null);
        }
        public Maybe()
        {
            Value = default(T);
            HasValue = false;
        }
        public override string ToString()
        {
            return HasValue ? Value.ToString() : "Nothing";
        }
    }
}
// ReSharper restore CompareNonConstrainedGenericWithNull