#define MAPPED_DATA_OPTIMIZE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Core
{
    public static class MappedData
    {

        static SortedItems<IMapDataBase> _Data = new SortedItems<IMapDataBase>() { Comparer = MapDataComparer<IMapDataBase>.Comparer };
        /// <summary>
        /// Размеченные данные
        /// </summary>
        public static IList<IMapDataBase> Data => _Data;

        /// <summary>
        /// Словарь строковых значений
        /// </summary>
        internal static SortedItems<IMapRecord> _ValuesDictionary = new SortedItems<IMapRecord>() { Comparer = MapRecord.DefaultComparer };

        /// <summary>
        /// Словарь методов
        /// </summary>
        static SortedItems<IMapRecord> _MethodsDictionary = new SortedItems<IMapRecord>() { Comparer = MapRecord.DefaultComparer };

        public static bool FindValueIndex(string value, out int index)
        {
            return _ValuesDictionary.Find(new MapRecord(value), out index);
            //return FindRecodIndex(_ValuesDictionary, value, out index);
        }

        public static IMapRecord AddNewValueIn(int idx, string value)
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
        public static IMapRecord GetValueRecord(string value)
        {
            int idx;
            if (FindValueIndex(value, out idx))
                return _ValuesDictionary[idx];
            return AddNewValueIn(idx, value);
        }

        /// <summary>
        /// Возвращает запись словаря по значению
        /// </summary>
        /// <param name="item">Элемент разметки</param>
        /// <returns>Запись из словаря, или null</returns>
        public static IMapRecord GetValueRecord(IMapRangeItem item)
        {
            var itm = item as IMapValueItem;
            if (itm == null)
                return null;
            return GetValueRecord(itm.Value);
        }

        public static bool FindMethodIndex(string value, out int index)
        {
            return _MethodsDictionary.Find(new MapRecord(value), out index);
            //return FindRecodIndex(_MethodsDictionary, value, out index);
        }

        public static IMapRecord AddNewMethodIn(int idx, string value)
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
        public static IMapRecord GetMethodRecord(string method)
        {
            int idx;
            if (FindMethodIndex(method, out idx))
                return _MethodsDictionary[idx];
            return AddNewMethodIn(idx, method);
        }

        /// <summary>
        /// Для проверки есть ли такое значение в словаре
        /// </summary>
        /// <param name="value">Искомое значение</param>
        /// <returns>True если есть</returns>
        public static bool IsValueExists(string value) => _ValuesDictionary.ContainsSorted(itm => string.Compare(value, itm.Value, false));

        /// <summary>
        /// Для удаления объекта разметки из записей ссылающихся на него
        /// </summary>
        static void RemoveRecordInfo<T>(IList<IMapRecord> lst, IMapData data, Func<string, IMapRecord> Getter) where T : IMapBaseItem
        {
            foreach (var item in new SortedSet<T>(data.Items.OfType<T>().ToArray(), MapBaseItemComparer<T>.Comparer))
                (Getter(item.Value) as IMapRecordFull).Data.Remove(data);
        }

        /// <summary>
        /// Удаляет размеченные данные из словарей
        /// </summary>
        /// <param name="data">Размеченные данные</param>
        static void RemoveMapInfo(IMapData data)
        {
            if (data.IsMapped)
            {
                RemoveRecordInfo<IMapValueItem>(_ValuesDictionary, data, GetValueRecord);
                RemoveRecordInfo<IMapMethodItem>(_MethodsDictionary, data, GetMethodRecord);
            }
        }

        /// <summary>
        /// Для добавления объекта разметки к записям соответствующим значениям разметки объекта
        /// </summary>
        static void AddRecordInfo<T>(IList<IMapRecord> lst, IMapData data, Func<string, IMapRecord> Getter) where T : IMapBaseItem
        {
            var items = data.Items.OfType<T>().ToArray();
#if MAPPED_DATA_OPTIMIZE
            var store = new List<IMapRecord>() { Capacity = items.Length };
#endif
            //SortedSet лчень быстро выкинет дубли и вернет чистенький сортированы энум
            foreach (var item in new SortedSet<T>(items, MapBaseItemComparer<T>.Comparer))
            {
                var rec = Getter(item.Value);
                (rec as IMapRecordFull).Data.Add(data);
#if MAPPED_DATA_OPTIMIZE
                store.Add(rec);
#endif
            }

#if MAPPED_DATA_OPTIMIZE
            foreach (var item in items.OfType<IMapOptimizableValueItem>())
                item.SwapValueToMapRecord(store[store.BinarySearch(new MapRecord(item.Value), MapRecord.DefaultComparer)]);
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
                AddRecordInfo<IMapValueItem>(_ValuesDictionary, data, GetValueRecord);
                AddRecordInfo<IMapMethodItem>(_MethodsDictionary, data, GetMethodRecord);
            }
        }

        /// <summary>
        /// Добавляет размеченные данные
        /// Если файл этой разметки уже есть то будет обновлен имеющийся
        /// иначе разметка будет добавлена и обновлена
        /// </summary>
        /// <param name="data">Размеченные данные</param>
        /// <param name="safe">Если не нужна синхронизация</param>
        /// <returns>Вернет переданную разметку, или найденную, если файл уже был добавлен</returns>
        public static IMapData AddData(IMapData data, bool safe)
        {
            int idx = _Data.IndexOf(data);
            if (idx < 0)
                _Data.Add(data);
            else
            {
                data = _Data[idx] as IMapData;
                if (safe)
                    RemoveMapInfo(data);
                else
                    Helpers.mainCTX.Send(_ => RemoveMapInfo(data), null);
            }
            data.Remap(false, safe);
            if (safe)
                AddMapInfo(data);
            else
                Helpers.mainCTX.Send(_ => AddMapInfo(data), null);
            return data;
        }

        /// <summary>
        /// Обновляет данные разметки
        /// </summary>
        /// <param name="data">Разметкп</param>
        /// <param name="safe">Если не нужна синхронизация</param>
        /// <param name="ifChanged">Только если файл изменен</param>
        public static void UpdateData(IMapData data, bool safe, bool ifChanged)
        {
            if (data != null && (!ifChanged || data.IsChanged))
            {
                RemoveMapInfo(data);
                data.Remap(ifChanged, safe);
                AddMapInfo(data);
            }
        }

        /// <summary>
        /// Обновляет данные разметки
        /// </summary>
        /// <param name="name">Путь к файлу</param>
        /// <param name="safe">Если не нужна синхронизация</param>
        /// <param name="ifChanged">Только если файл изменен</param>
        public static void UpdateData(string name, bool safe, bool ifChanged)
        {
            UpdateData(GetData(name), safe, ifChanged);
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

        /// <summary>
        /// Удаляет разметку по пути к файлу
        /// </summary>
        /// <param name="name">Путь к файлу</param>
        public static void RemoveData(string name)
        {
            var data = GetData(name);
            if (data == null)
                return;
            RemoveMapInfo(data);
            _Data.Remove(data);
        }

        /// <summary>
        /// Ищет Разметку по пути к файлу
        /// </summary>
        /// <param name="name">Путь к файлу</param>
        /// <returns>Разметка если найдена или null</returns>
        public static IMapData GetData(string name) => _Data.GetSorted(itm => string.Compare(name, itm.FullPath, true)) as IMapData;

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
    }
}
