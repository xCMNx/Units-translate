using System;
using System.ComponentModel;

namespace Core
{
    /// <summary>
    /// Структура для словаря размеченных методов, содержит название метода и связанные с ним размеченные данные
    /// </summary>
    public struct MapMethodRecord : INotifyPropertyChanged, IMapMethodRecord
    {
        string value;
        public string Value => value;
        SortedObservableCollection<IMapData> data;
        public ISortedObservableCollection<IMapData> Data => data;
        bool needCalc;
        int _Count;
        public int Count => needCalc ? _Count = GetCount() : _Count;
        public MapMethodRecord(string val)
        {
            value = val;
            data = new SortedObservableCollection<IMapData>();
            _Count = 0;
            needCalc = false;
            PropertyChanged = null;
            data.CollectionChanged += Data_CollectionChanged;
        }

        int GetCount()
        {
            var cnt = 0;
            foreach (var itm in data)
                cnt += itm.GetItemsCountWithValue(this);
            return cnt;
        }

        private void Data_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            needCalc = true;
            NotifyPropertyChanged(nameof(Count));
        }

        public int CompareTo(string obj) => string.Compare(Value, obj, StringComparison.Ordinal);
        public int CompareTo(IMapMethodRecord obj) => CompareTo(obj.Value);
        public int CompareTo(object obj) => obj is string ? CompareTo((string)obj) : CompareTo((obj as IMapMethodRecord)?.Value);
        public bool Equals(string other) => CompareTo(other) == 0;
        public bool Equals(IMapRecord other) => CompareTo(other) == 0;

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public override string ToString()
        {
            return $"[{Value}]";
        }
    }
}
