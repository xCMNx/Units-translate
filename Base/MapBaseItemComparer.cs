using System;
using System.Collections.Generic;

namespace Core
{
    public class MapBaseItemComparer<T> : Comparer<T> where T : IMapBaseItem
    {
        public override int Compare(T x, T y)
        {
            return string.Compare(x.Value, y.Value, StringComparison.Ordinal);
        }

        public static readonly MapBaseItemComparer<T> Comparer = new MapBaseItemComparer<T>();
    }
}
