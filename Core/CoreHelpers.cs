using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Core
{
    /// <summary>
    /// Компарер умеющий сравнивать размеченные данные
    /// Сравнивает полные пути
    /// </summary>
    public class MapDataComparer : Comparer<IMapDataBase>
    {
        public override int Compare(IMapDataBase m1, IMapDataBase m2) => string.Compare(m1?.FullPath, m2?.FullPath, true);

        public static readonly MapDataComparer Comparer = new MapDataComparer();
    }

    public interface IMapRecord : IComparable
    {
        string Value { get; }
    }

    public interface IMapRecordFull : IMapRecord
    {
        SortedObservableCollection<IMapData> Data { get; }
        int Count { get; }
    }
    public interface IMapValueRecord : IMapRecordFull, INotifyPropertyChanged
    {
        string Translation { get; set; }
    }
    /// <summary>
    /// Структура для словаря размеченных значений, содержит само значение
    /// Используется для поиска
    /// </summary>
    public struct MapRecord : IMapRecord
    {
        string value;
        public string Value { get { return value; } }

        public MapRecord(string val)
        {
            value = val;
        }

        public int CompareTo(object obj)
        {
            var rec = obj as IMapRecord;
            return rec == null ? -1 : Value.CompareTo(rec.Value);
        }
    }

    /// <summary>
    /// Структура для словаря размеченных методов, содержит название метода и связанные с ним размеченные данные
    /// </summary>
    public struct MapMethodRecord : INotifyPropertyChanged, IMapRecordFull
    {
        string value;
        public string Value => value;
        SortedObservableCollection<IMapData> data;
        public SortedObservableCollection<IMapData> Data => data;

        bool needCalc;
        int _Count;
        public int Count => needCalc ? _Count = GetCount() : _Count;

        public MapMethodRecord(string val)
        {
            value = val;
            data = new SortedObservableCollection<IMapData>() { Comparer = MapDataComparer.Comparer };
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
    }

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
            get { return translation; }
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
        public SortedObservableCollection<IMapData> Data => data;

        public MapValueRecord(string val, string trans)
        {
            value = val;
            translation = trans;
            data = new SortedObservableCollection<IMapData>() { Comparer = MapDataComparer.Comparer };
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

        public MapValueRecord(string val) : this(val, string.Empty)
        {
        }

        public int CompareTo(object obj)
        {
            var rec = obj as IMapRecord;
            return rec == null ? -1 : Value.CompareTo(rec.Value);
        }
    }

    /// <summary>
    /// Компарер для словаря размеченных значений
    /// Сравнивает непосредственно значения, чувствителен к регистру
    /// </summary>
    public class MapRecordComparer : Comparer<IMapRecord>
    {
        public override int Compare(IMapRecord m1, IMapRecord m2) => string.Compare(m1.Value, m2.Value, false);

        public static readonly MapRecordComparer Comparer = new MapRecordComparer();
    }

    public class TranslationItemEqualityComparer : EqualityComparer<ITranslationItem>
    {
        public override bool Equals(ITranslationItem x, ITranslationItem y)
        {
            return string.Equals(x.Value, y.Value);
        }

        public override int GetHashCode(ITranslationItem obj)
        {
            return obj == null ? 0 : obj.Value.GetHashCode();
        }

        public static readonly TranslationItemEqualityComparer EqualityComparer = new TranslationItemEqualityComparer();
    }
}
