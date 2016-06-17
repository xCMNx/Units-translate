using System;
using System.ComponentModel;

namespace Core
{
    /// <summary>
    /// Структура для словаря размеченных методов, содержит название метода и связанные с ним размеченные данные
    /// </summary>
    public struct MapMethodRecord : INotifyPropertyChanged, IMapRecordFull
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
            data = new SortedObservableCollection<IMapData>()
            {Comparer = MapDataComparer<IMapData>.Comparer};
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

        public int CompareTo(object obj)
        {
            var rec = obj as IMapRecord;
            return rec == null ? -1 : Value.CompareTo(rec.Value);
        }

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
