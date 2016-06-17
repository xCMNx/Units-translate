using System;

namespace Core
{
    /// <summary>
    /// Структура для словаря размеченных значений, содержит значение и связанные с ним размеченные данные
    /// </summary>
    public class MapValueRecord : BindableBase, IMapValueRecord
    {
        string value;
        string translation;
        public string Value => value;
        public string Translation
        {
            get
            {
                return translation;
            }

            set
            {
                translation = value;
                NotifyPropertyChanged(nameof(Translation));
            }
        }

        bool needCalc = false;
        int _Count = 0;
        public int Count => needCalc ? _Count = GetCount() : _Count;
        SortedObservableCollection<IMapData> data;
        public ISortedObservableCollection<IMapData> Data => data;
        public MapValueRecord(string val, string trans)
        {
            value = val;
            translation = trans;
            data = new SortedObservableCollection<IMapData>()
            {Comparer = MapDataComparer<IMapData>.Comparer};
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

        public MapValueRecord(string val): this (val, string.Empty)
        {
        }

        public int CompareTo(object obj)
        {
            var rec = obj as IMapRecord;
            return rec == null ? -1 : string.Compare(Value, rec.Value, StringComparison.Ordinal);
        }

        public override string ToString()
        {
            return Value;
        }
    }
}
