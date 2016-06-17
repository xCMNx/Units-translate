using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Core
{
    public interface ISortedObservableCollection<T>: ICollection<T>, IList<T>, IEnumerable<T>, INotifyCollectionChanged, INotifyPropertyChanged
    {
    }
}
