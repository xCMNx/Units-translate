using System.Collections.Generic;

namespace Core
{
    public interface ITranslationItem
    {
        string Value { get; }
        string Translation { get; }
    }

    public class TranslationItemEqualityComparer<T> : EqualityComparer<T> where T: ITranslationItem
    {
        public override bool Equals(T x, T y)
        {
            return string.Equals(x.Value, y.Value);
        }

        public override int GetHashCode(T obj)
        {
            return obj == null ? 0 : obj.Value.GetHashCode();
        }

        public static readonly TranslationItemEqualityComparer<T> EqualityComparer = new TranslationItemEqualityComparer<T>();
    }
}
