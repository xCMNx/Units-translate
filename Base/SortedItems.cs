using System.Collections.Generic;

namespace Core
{
    public class SortedItems<T> : List<T>
    {

        public bool Find(T item, out int index, IComparer<T> comparer = null)
        {
            index = BinarySearch(item, comparer ?? Comparer);
            if (index < 0)
            {
                index = ~index;
                return false;
            }
            return true;
        }

        public IComparer<T> Comparer;

        public int Add(T item, IComparer<T> comparer = null)
        {
            int index = BinarySearch(item, comparer);
            if (index < 0)
            {
                index = ~index;
                Insert(index, item);
            }
            return index;
        }

        public new int Add(T item) => Add(item, Comparer);

        public void Remove(T item, IComparer<T> comparer = null)
        {
            int index = BinarySearch(item, comparer ?? Comparer);
            if (index >= 0)
                RemoveAt(index);
        }

        public new void Remove(T item) => Remove(item, Comparer);

        public int IndexOf(T item, IComparer<T> comparer = null)
        {
            return BinarySearch(item, comparer);
        }

        public new int IndexOf(T item)
        {
            return BinarySearch(item, Comparer);
        }

        public bool Contain(T item)
        {
            return BinarySearch(item, Comparer) >= 0;
        }
    }
}
