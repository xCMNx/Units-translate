using System.Collections.Generic;

namespace Core
{
    public class SortedObservableCollection<T> : ObservableCollectionEx<T>
    {
        public bool Find(T item, out int index)
        {
            return (index = BinarySearch(item)) >= 0;
        }

        public int BinarySearch(T item, IComparer<T> comparer)
        {
            return (Items as List<T>).BinarySearch(item, comparer);
        }

        public int BinarySearch(T item)
        {
            return BinarySearch(item, Comparer);
        }

        public IComparer<T> Comparer;

        public new int Add(T item)
        {
            int index = BinarySearch(item);
            if (index < 0)
            {
                index = ~index;
                Insert(index, item);
            }
            return index;
        }

        public new void Remove(T item)
        {
            int index = BinarySearch(item);
            if (index >= 0)
                RemoveAt(index);
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
