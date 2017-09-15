#define MAPPED_DATA_OPTIMIZE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Core
{
    public static class MappedData
    {

        internal static SortedItems<IComparable> _Data = new SortedItems<IComparable>();
        /// <summary>
        /// Размеченные данные
        /// </summary>
        public static IEnumerable<IMapData> Data => _Data.OfType<IMapData>();

        /// <summary>
        /// Словарь строковых значений
        /// </summary>
        internal static SortedItems<IComparable> _ValuesDictionary = new SortedItems<IComparable>();

        /// <summary>
        /// Словарь методов
        /// </summary>
        static SortedItems<IComparable> _MethodsDictionary = new SortedItems<IComparable>();

        static IMapValueRecord AddNewValueIn(int idx, string value)
        {
            var n = new MapValueRecord(value);
            _ValuesDictionary.Insert(idx, n);
            return n;
        }

        /// <summary>
        /// Возвращает запись словаря по значению
        /// </summary>
        /// <param name="value">Значение</param>
        /// <returns>Запись из словаря, или null</returns>
        public static IMapValueRecord GetValueRecord(string value)
        {
            int idx;
            if (_ValuesDictionary.Find(value, out idx))
                return (IMapValueRecord)_ValuesDictionary[idx];
            return AddNewValueIn(idx, value);
        }

        static IMapMethodRecord AddNewMethodIn(int idx, string value)
        {
            var n = new MapMethodRecord(value);
            _MethodsDictionary.Insert(idx, n);
            return n;
        }

        /// <summary>
        /// Возвращает запись словаря по имени метода
        /// </summary>
        /// <param name="method">Имя метода</param>
        /// <returns>Запись из словаря, или null</returns>
        public static IMapMethodRecord GetMethodRecord(string method)
        {
            int idx;
            if (_MethodsDictionary.Find(method, out idx))
                return (IMapMethodRecord)_MethodsDictionary[idx];
            return AddNewMethodIn(idx, method);
        }

        /// <summary>
        /// Для проверки есть ли такое значение в словаре
        /// </summary>
        /// <param name="value">Искомое значение</param>
        /// <returns>True если есть</returns>
        public static bool IsValueExists(string value) => _ValuesDictionary.IndexOf(value) >= 0;

        /// <summary>
        /// Для удаления объекта разметки из записей ссылающихся на него
        /// </summary>
        static void RemoveRecordInfo<T>(IEnumerable<IMapRecordFull> lst, IMapData data, Func<string, IMapRecordFull> Getter) where T : IMapBaseItem
        {
            foreach (var item in new SortedSet<T>(data.Items.OfType<T>().ToArray(), MapBaseItemComparer<T>.Comparer))
                Getter(item.Value).Data.Remove(data);
        }

        /// <summary>
        /// Удаляет размеченные данные из словарей
        /// </summary>
        /// <param name="data">Размеченные данные</param>
        static void RemoveMapInfo(IMapData data)
        {
            if (data.IsMapped)
            {
                RemoveRecordInfo<IMapValueItem>(_ValuesDictionary.OfType<IMapRecordFull>(), data, GetValueRecord);
                RemoveRecordInfo<IMapMethodItem>(_MethodsDictionary.OfType<IMapRecordFull>(), data, GetMethodRecord);
            }
        }

        /// <summary>
        /// Для добавления объекта разметки к записям соответствующим значениям разметки объекта
        /// </summary>
        static void AddRecordInfo<T>(IEnumerable<IMapRecordFull> lst, IMapData data, Func<string, IMapRecordFull> Getter) where T : IMapBaseItem
        {
            var items = data.Items.OfType<T>().ToArray();
#if MAPPED_DATA_OPTIMIZE
            var store = new List<IComparable>() { Capacity = items.Length };
#endif
            //SortedSet лчень быстро выкинет дубли и вернет чистенький сортированы энум
            foreach (var item in new SortedSet<T>(items, MapBaseItemComparer<T>.Comparer))
            {
                var rec = Getter(item.Value);
                (rec as IMapRecordFull).Data.Add(data);
                if (rec is IMapValueRecord mvr)
                    mvr.WasLinked = true;
#if MAPPED_DATA_OPTIMIZE
                store.Add(rec);
#endif
            }

#if MAPPED_DATA_OPTIMIZE
            foreach (var item in items.OfType<IMapOptimizableValueItem>())
                item.SwapValueToMapRecord((IMapRecord)store[store.BinarySearch(item.Value)]);
#endif
        }

        /// <summary>
        /// Добавляет размеченные данные в словари
        /// </summary>
        /// <param name="data">Размеченные данные</param>
        static void AddMapInfo(IMapData data)
        {
            if (data.IsMapped)
            {
                AddRecordInfo<IMapValueItem>(_ValuesDictionary.OfType<IMapRecordFull>(), data, GetValueRecord);
                AddRecordInfo<IMapMethodItem>(_MethodsDictionary.OfType<IMapRecordFull>(), data, GetMethodRecord);
            }
        }

        /// <summary>
        /// Добавляет размеченные данные
        /// Если файл этой разметки уже есть то будет обновлен имеющийся
        /// иначе разметка будет добавлена и обновлена
        /// </summary>
        /// <param name="data">Размеченные данные</param>
        /// <returns>Вернет переданную разметку, или найденную, если файл уже был добавлен</returns>
        public static IMapData AddData(IMapData data)
        {
            var idx = _Data.IndexOf(data);
            if (idx < 0)
                _Data.Add(data);
            else
            {
                data = _Data[idx] as IMapData;
                RemoveMapInfo(data);
            }
            data.Remap(false);
            AddMapInfo(data);
            return data;
        }

        /// <summary>
        /// Обновляет данные разметки
        /// </summary>
        /// <param name="data">Разметкп</param>
        /// <param name="ifChanged">Только если файл изменен</param>
        public static void UpdateData(IMapData data, bool ifChanged)
        {
            if (data != null && (!ifChanged || data.IsChanged) && _Data.Contains(data))
            {
                RemoveMapInfo(data);
                data.Remap(ifChanged);
                AddMapInfo(data);
            }
        }

        /// <summary>
        /// Удаляет разметку
        /// </summary>
        /// <param name="data">Разметка</param>
        public static void RemoveData(IMapData data)
        {
            RemoveMapInfo(data);
            _Data.Remove(data);
        }

        public static string RegexCompareExpression(this IMapValueRecord rec)
        {
            var vVal = rec.Value.Trim();
            var vTr = rec.Translation?.Trim();
            return string.Format(@"(?s)^\s*{0}\s*$", Regex.Escape(
                string.IsNullOrWhiteSpace(vVal) ?
                    vTr :
                    string.IsNullOrWhiteSpace(vTr) ?
                        vVal :
                        string.Format("{0}|{1}", vVal, vTr)
            ));
        }

        public static ICollection<T> GetAnalogs<T>(this IMapValueRecord rec) where T : IMapRecord
        {
            if (rec == null || (string.IsNullOrWhiteSpace(rec.Value) && string.IsNullOrWhiteSpace(rec.Translation)))
                return new T[0];
            var rgxp = new Regex(rec.RegexCompareExpression(), RegexOptions.IgnoreCase);
            return _ValuesDictionary.OfType<IMapValueRecord>().Where(v => rgxp.IsMatch(v.Value) || rgxp.IsMatch(v.Translation)).Except(new IMapValueRecord[] { rec }).OfType<T>().ToArray();
        }

        public static void Clear()
        {
            var lst = _Data.OfType<IMapData>().ToArray();
            _Data.Clear();
            foreach (var d in lst)
                d.Dispose();
        }
    }
}
