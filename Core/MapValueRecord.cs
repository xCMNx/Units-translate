﻿using System;

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

        public bool WasLinked { get; set; } = false;

        bool needCalc = false;
        int _Count = 0;
        public int Count => needCalc ? _Count = GetCount() : _Count;
        SortedObservableCollection<IMapData> data;
        public ISortedObservableCollection<IMapData> Data => data;
        public MapValueRecord(string val, string trans)
        {
            value = val;
            translation = trans;
            data = new SortedObservableCollection<IMapData>();
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

        public override string ToString() => Value;
        public string ToStringFull => $"{Value}\r\n\r\n{Translation}";

        public int CompareTo(string obj) => string.Compare(Value, obj, StringComparison.Ordinal);
        public int CompareTo(IMapValueRecord obj) => CompareTo(obj.Value);
        public int CompareTo(object obj) => obj is string ? CompareTo((string)obj) : CompareTo((obj as IMapValueRecord)?.Value);
        public bool Equals(string other) => CompareTo(other) == 0;
        public bool Equals(IMapRecord other) => CompareTo(other.Value) == 0;
    }
}
