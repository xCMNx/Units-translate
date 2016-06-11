using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Core
{
    public static class MappedData
    {

        static SortedItems<IMapDataBase> _Data = new SortedItems<IMapDataBase>() { Comparer = MapDataComparer.Comparer };
        /// <summary>
        /// Размеченные данные
        /// </summary>
        public static IList<IMapDataBase> Data => _Data;

        /// <summary>
        /// Словарь строковых значений
        /// </summary>
        internal static SortedItems<IMapRecord> _ValuesDictionary = new SortedItems<IMapRecord>() { Comparer = MapRecordComparer.Comparer };

        /// <summary>
        /// Словарь методов
        /// </summary>
        static SortedItems<IMapRecord> _MethodsDictionary = new SortedItems<IMapRecord>() { Comparer = MapRecordComparer.Comparer };

        /// <summary>
        /// Возвращает запись словаря по значению
        /// </summary>
        /// <param name="value">Значение</param>
        /// <returns>Запись из словаря, или null</returns>
        public static IMapRecord GetValueRecord(string value)
        {
            var idx = _ValuesDictionary.IndexOf(new MapRecord(value));
            if (idx < 0)
                return _ValuesDictionary[_ValuesDictionary.Add(new MapValueRecord(value))];
            return _ValuesDictionary[idx];
        }

        /// <summary>
        /// Возвращает запись словаря по значению
        /// </summary>
        /// <param name="item">Элемент разметки</param>
        /// <returns>Запись из словаря, или null</returns>
        public static IMapRecord GetValueRecord(IMapItemRange item)
        {
            var itm = item as IMapValueItem;
            if (itm == null)
                return null;
            return GetValueRecord(itm.Value);
        }

        /// <summary>
        /// Возвращает запись словаря по имени метода
        /// </summary>
        /// <param name="method">Имя метода</param>
        /// <returns>Запись из словаря, или null</returns>
        public static IMapRecord GetMethodRecord(string method)
        {
            var idx = _MethodsDictionary.IndexOf(new MapRecord(method));
            if (idx < 0)
                return _MethodsDictionary[_MethodsDictionary.Add(new MapMethodRecord(method))];
            return _MethodsDictionary[idx];
        }

        /// <summary>
        /// Возвращает запись словаря
        /// </summary>
        /// <param name="item">Элемент разметки</param>
        /// <returns>Запись из словаря, или null</returns>
        public static IMapRecord GetAnyRecord(IMapItemRange item)
        {
            var itmv = item as IMapValueItem;
            if (itmv != null)
                return GetValueRecord(itmv.Value);
            var itmm = item as IMapMethodItem;
            if (itmm != null)
                return GetMethodRecord(itmm.Value);
            return null;
        }

        /// <summary>
        /// Для проверки есть ли такое значение в словаре
        /// </summary>
        /// <param name="value">Искомое значение</param>
        /// <returns>True если есть</returns>
        public static bool IsValueExists(string value)
        {
            return _ValuesDictionary.IndexOf(new MapRecord(value)) >= 0;
        }

        /// <summary>
        /// Удаляет размеченные данные из словарей
        /// </summary>
        /// <param name="data">Размеченные данные</param>
        static void RemoveMapInfo(IMapData data)
        {
            if (data.IsMapped)
                foreach (var item in data.Items)
                {
                    var vr = GetAnyRecord(item) as IMapRecordFull;
                    if (vr != null)
                        vr.Data.Remove(data);
                }
        }

        /// <summary>
        /// Добавляет размеченные данные в словари
        /// </summary>
        /// <param name="data">Размеченные данные</param>
        static void AddMapInfo(IMapData data)
        {
            if (data.IsMapped)
                foreach (var item in data.Items)
                {
                    var vr = GetAnyRecord(item) as IMapRecordFull;
                    if (vr != null)
                        vr.Data.Add(data);
                }
        }

        /// <summary>
        /// Добавляет размеченные данные
        /// Если файл этой разметки уже есть то будет обновлен имеющийся
        /// иначе разметка булет добавлена и обновлена
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
            if (data != null)
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
            var idx = _Data.IndexOf(new Dummy() { fullpath = name });
            if (idx < 0)
                return;
            var data = _Data[idx] as IMapData;
            RemoveMapInfo(data);
            _Data.Remove(data);
        }

        /// <summary>
        /// Пустышка для поиска файла
        /// </summary>
        struct Dummy : IMapDataBase
        {
            public string fullpath;
            public string FullPath { get { return fullpath; } }
        }

        /// <summary>
        /// Ищет Разметку по пути к файлу
        /// </summary>
        /// <param name="name">Путь к файлу</param>
        /// <returns>Разметка если найдена или null</returns>
        public static IMapData GetData(string name)
        {
            var idx = _Data.IndexOf(new Dummy() { fullpath = name });
            if (idx < 0)
                return null;
            return _Data[idx] as IMapData;
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
    }
}
