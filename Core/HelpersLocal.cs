using System.Collections.Generic;
using System.Linq;

namespace Core
{
    public static partial class Helpers
    {
        public static int IndexOfReader<T, T2>(this IList<KeyValuePair<T, T2>> lst, string name) where T2: IRepresentationAttribute
        {
            for (var idx = 0; idx < lst.Count; idx++)
                if (lst[idx].Value.Name == name)
                    return idx;
            return -1;
        }

        public static T GetReader<T, T2>(this IList<KeyValuePair<T, T2>> lst, int index) where T2 : IRepresentationAttribute
        {
            return lst[index].Key;
        }

        public static string GetReaderName<T, T2>(this IList<KeyValuePair<T, T2>> lst, int index) where T2 : IRepresentationAttribute
        {
            return lst[index].Value.Name;
        }

        public static string Filter<T, T2>(this IList<KeyValuePair<T, T2>> lst) where T2 : IRepresentationAttribute => string.Join("|", lst.Select(s => s.Value.Filter));
    }
}
