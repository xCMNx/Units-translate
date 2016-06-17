using System;
using System.Collections.Generic;

namespace Core
{
    /// <summary>
    /// Компарер для словаря размеченных значений
    /// Сравнивает непосредственно значения, чувствителен к регистру
    /// </summary>
    public class MapRecordComparer<T> : Comparer<T> where T : IMapRecord
    {
        public override int Compare(T m1, T m2) => string.Compare(m1.Value, m2.Value, StringComparison.Ordinal);
        public static readonly MapRecordComparer<T> Comparer = new MapRecordComparer<T>();
    }

    /// <summary>
    /// Структура для словаря размеченных значений, содержит само значение
    /// Используется для поиска
    /// </summary>
    public struct MapRecord : IMapRecord
    {
        string value;
        public string Value => value;
        public MapRecord(string val)
        {
            value = val;
        }

        public static IComparer<IMapRecord> DefaultComparer = MapRecordComparer<IMapRecord>.Comparer;

        public int CompareTo(object obj)
        {
            var rec = obj as IMapRecord;
            return rec == null ? -1 : string.Compare(Value, rec.Value, StringComparison.Ordinal);
        }
    }
}
