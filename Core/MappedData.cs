using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace Core
{
    #region Вспомогательные классы и интерфейсы
    /// <summary>
    /// Компарер умеющий сравнивать размеченные данные
    /// Сравнивает полные пути
    /// </summary>
    public class MapDataComparer : Comparer<IMapData>
    {
        public override int Compare(IMapData m1, IMapData m2)
        {
            return string.Compare(m1?.FullPath, m2?.FullPath, true);
        }

        public static readonly MapDataComparer Comparer = new MapDataComparer();
    }

    public interface IMapRecord
    {
        string Value { get; }
    }
    public interface IMapRecordFull : IMapRecord
    {
        SortedObservableCollection<IMapData> Data { get; }
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
    }

    /// <summary>
    /// Структура для словаря размеченных значений, содержит значение и связанные с ним размеченные данные
    /// </summary>
    public struct MapRecordFull : IMapRecordFull
    {
        string value;
        string translation;
        public string Value => value;
        public string Translation
        {
            get { return translation; }
            set { translation = value; }
        }

        SortedObservableCollection<IMapData> data;
        public SortedObservableCollection<IMapData> Data => data;

        public MapRecordFull(string val, string trans)
        {
            value = val;
            translation = trans;
            data = new SortedObservableCollection<IMapData>() { Comparer = MapDataComparer.Comparer };
        }

        public MapRecordFull(string val) : this(val, string.Empty)
        {
        }
    }

    /// <summary>
    /// Компарер для словаря размеченных значений
    /// Сравнивает непосредственно значения, чувствителен к регистру
    /// </summary>
    public class MapRecordComparer : Comparer<IMapRecord>
    {
        public override int Compare(IMapRecord m1, IMapRecord m2)
        {
            return string.Compare(m1.Value, m2.Value, false);
        }

        public static readonly MapRecordComparer Comparer = new MapRecordComparer();
    }

    #endregion
    public static class MappedData
    {

        static SortedItems<IMapData> _Data = new SortedItems<IMapData>() { Comparer = MapDataComparer.Comparer };
        /// <summary>
        /// Размеченные данные
        /// </summary>
        public static IList<IMapData> Data => _Data;

        /// <summary>
        /// Словарь строковых значений
        /// </summary>
        static SortedItems<IMapRecord> _ValuesDictionary = new SortedItems<IMapRecord>() { Comparer = MapRecordComparer.Comparer };

        static SortedObservableCollection<IMapRecord> _TranslatesDictionary = new SortedObservableCollection<IMapRecord>() { Comparer = MapRecordComparer.Comparer };
        /// <summary>
        /// Словарь переводов
        /// </summary>
        public static SortedObservableCollection<IMapRecord> TranslatesDictionary => _TranslatesDictionary;
        /// <summary>
        /// Словарь переводов которые связаны с файлами
        /// </summary>
        public static IEnumerable<IMapRecord> UsedTranslates => _TranslatesDictionary.Where(tr => ((IMapRecordFull)tr).Data.Count > 0);

        /// <summary>
        /// Возвращает запись словаря по значению
        /// Реализовано хранение только строк, остальные значения игнорируются
        /// </summary>
        /// <param name="value">Значение</param>
        /// <returns>Запись из словаря, или null для областей не являющихся строками</returns>
        public static IMapRecord GetValueRecord(string value)
        {
            var idx = _ValuesDictionary.IndexOf(new MapRecord(value));
            if (idx < 0)
                return _ValuesDictionary[_ValuesDictionary.Add(new MapRecordFull(value))];
            return _ValuesDictionary[idx];
        }

        /// <summary>
        /// Возвращает запись словаря по значению
        /// Реализовано хранение только строк, остальные значения игнорируются
        /// </summary>
        /// <param name="item">Элемент разметки</param>
        /// <returns>Запись из словаря, или null для областей не являющихся строками</returns>
        public static IMapRecord GetValueRecord(IMapItemRange item)
        {
            var itm = item as IMapValueItem;
            if (itm == null)
                return null;
            return GetValueRecord(itm.Value);
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
                    var vr = GetValueRecord(item) as IMapRecordFull;
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
                    var vr = GetValueRecord(item) as IMapRecordFull;
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
                data = _Data[idx];
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
            var data = _Data[idx];
            RemoveMapInfo(data);
            _Data.Remove(data);
        }

        /// <summary>
        /// Пустышка для поиска файла
        /// </summary>
        struct Dummy : IMapData
        {
            public string fullpath;
            public string Ext { get { return null; } }
            public string FullPath { get { return fullpath; } }
            public bool IsMapped { get { return true; } }
            public IEnumerable<IMapItemRange> Items { get { return null; } }
            public string Name { get { return null; } }
            public string Path { get { return null; } }
            public string Text { get { return null; } }
            public event PropertyChangedEventHandler PropertyChanged;
            public void ClearItems() {}
            public IMapValueItem ValueItemAt(int index) => null;
            public IEnumerable<IMapItemRange> ItemsBetween(int start, int end) => null;
            public void Remap(bool ifChanged, bool safe) {}
            public void SaveText(string text) {}
            public IList<IMapItemRange> ItemsAt(int index) => null;
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
            return _Data[idx];
        }

        /// <summary>
        /// Очищает переводы
        /// </summary>
        public static void ClearTranslates()
        {
            _TranslatesDictionary.Clear();
            foreach (IMapRecordFull it in _ValuesDictionary)
                it.Translation = string.Empty;
        }

        static Linguist OriginalData = new Linguist();
        public static Encoding encoding = Encoding.GetEncoding(Helpers.Default_Encoding);
        static XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
        static XmlWriterSettings WriterSettings = new XmlWriterSettings { Indent = true, IndentChars = "\t", NewLineHandling = NewLineHandling.None };
        static XmlSerializer serializer = new XmlSerializer(typeof(Linguist));

        /// <summary>
        /// Загружает новые данные переводов
        /// </summary>
        /// <param name="path">Путь к файлу переводов</param>
        /// <param name="onTranslationConflict">
        /// Вызывается при конфликтах перевода.
        /// Получает текущую запись и конликтующий перевод.
        /// </param>
        public static void LoadTranslations(string path, Action<IMapRecordFull, string> onTranslationConflict)
        {
            using (var txtreader = new FileStream(path, FileMode.Open))
            {
                using (var xmlreader = new XmlTextReader(txtreader))
                {
                    xmlreader.MoveToContent();
                    encoding = xmlreader.Encoding;

                    OriginalData = (Linguist)serializer.Deserialize(xmlreader);
                }
            }

            var data = OriginalData.Entryes.OrderBy(e => e.Eng);
            var lst = new SortedItems<IMapRecordFull>() { Comparer = MapRecordComparer.Comparer };

            int repeatCnt = 0;
            int conflictsCnt = 0;
            foreach (var entry in data)
            {
                var item = (IMapRecordFull)GetValueRecord(entry.Eng);
                if (lst.Contain(item))
                {
                    if (string.Equals(item.Translation, entry.Trans))
                        repeatCnt++;
                    else
                    {
                        conflictsCnt++;
                        Helpers.ConsoleWrite("Конфликтующая запись перевода:", ConsoleColor.Yellow);
                        Helpers.ConsoleWrite(item.Value, ConsoleColor.White);
                        Helpers.ConsoleWrite(item.Translation, ConsoleColor.White);
                        Helpers.ConsoleWrite(entry.Trans, ConsoleColor.Gray);
                        if (onTranslationConflict != null)
                            onTranslationConflict(item, entry.Trans);
                    }
                    continue;
                }
                item.Translation = entry.Trans;
                lst.Add(item);
            }

            Helpers.ConsoleWrite(string.Format("Повторяющихся записей: {0}\r\nДублирующих записей: {1}", repeatCnt, conflictsCnt), ConsoleColor.Yellow);
            _TranslatesDictionary.Reset(lst);
        }

        /// <summary>
        /// Сохраняет данные переводов
        /// Если переводы были загружены ранее, то параметры файла будут взяты из оригинала
        /// </summary>
        /// <param name="path">Путь к файлу переводов</param>
        /// <param name="addnew">Добвить строки с переводом отсутствующие в оригинале</param>
        /// <param name="removeempty">Удалить строки на которые нет ссылок</param>
        public static void SaveTranslations(string path, bool addnew, bool removeempty)
        {
            OriginalData.Entryes.Clear();
            foreach (IMapRecordFull it in _TranslatesDictionary)
                if (!removeempty || it.Data.Count > 0)
                    OriginalData.Entryes.Add(new Entry(it.Value, it.Translation));
            if (addnew)
                foreach (IMapRecordFull it in _ValuesDictionary.Except(_TranslatesDictionary))
                    if (it.Data.Count > 0 && !string.IsNullOrWhiteSpace(it.Translation))
                        OriginalData.Entryes.Add(new Entry(it.Value, it.Translation));
            OriginalData.Entryes = OriginalData.Entryes.OrderBy(ent => ent.Eng).ToList();

            using (StreamWriter sw = new StreamWriter(path, false, encoding))
                using (var xw = XmlWriter.Create(sw, WriterSettings))
                {
                    xw.WriteStartDocument(true);
                    //xw.WriteRaw(string.Format("\r\n<?xml-stylesheet type=\"text/xsl\" href=\"{0}.xsl\"?>\r\n", Path.GetFileNameWithoutExtension(path)));
                    xw.WriteRaw("\r\n<?xml-stylesheet type=\"text/xsl\" href=\"eng_rus.xsl\"?>\r\n");
                    serializer.Serialize(xw, OriginalData, namespaces);
                }
        }

        [Flags]
        public enum SearchParams { EngOrTrans = 0, Eng = 1, Trans = 2, Both = 3}

        /// <summary>
        /// Поиск в словаре. Использует регулярку
        /// </summary>
        /// <param name="expr">Искомое выражение</param>
        /// <param name="param">Параметры поиска</param>
        /// <returns>Список совпадающих записей словаря</returns>
        public static IEnumerable<IMapRecord> Search(string expr, SearchParams param)
        {
            if (string.IsNullOrWhiteSpace(expr))
                return null;
            var res = new List<IMapRecord>();
            try
            {
                var rgxp = new Regex(expr);
                Func<IMapRecordFull, bool> cmpr = null;
                if(param.HasFlag(SearchParams.Eng))
                    if (param.HasFlag(SearchParams.Trans))
                        cmpr = it => rgxp.IsMatch(it.Value) && rgxp.IsMatch(it.Translation);
                    else
                        cmpr = it => rgxp.IsMatch(it.Value);
                else if (param.HasFlag(SearchParams.Trans))
                        cmpr = it => rgxp.IsMatch(it.Translation);
                    else
                        cmpr = it => rgxp.IsMatch(it.Value) || rgxp.IsMatch(it.Translation);
                foreach (IMapRecordFull it in _ValuesDictionary)
                    if (it.Data.Count > 0 && cmpr(it))
                        res.Add(it);
            }
            catch(Exception e)
            {
                Helpers.ConsoleWrite(e.ToString(), ConsoleColor.Green);
            }
            return res;
        }

        /// <summary>
        /// Поиск в словаре. Использует регулярку
        /// </summary>
        /// <param name="expr">
        /// Искомое выражение, в начале можно указать параметры поиска в фармате ?[params]:[expr]
        /// <para>?e:[expr]   - вырожению должна соответствовать строка</para>
        /// <para>?t:[expr]   - вырожению должен соответствовать перевод</para>
        /// <para>?et:[expr]  - вырожению должны соответствовать и строка и её перевод</para>
        /// <para>?:[expr]    - вырожению должны соответствовать или строка или перевод</para>
        /// </param>
        /// <returns>Список совпадающих записей словаря</returns>
        public static IEnumerable<IMapRecord> Search(string expr)
        {
            if (expr.StartsWith("?"))
            {
                var i = expr.IndexOf(':');
                if (i < 0)
                    return null;

                SearchParams param = SearchParams.EngOrTrans;
                var prm = expr.Substring(0, i).ToLower();
                if (prm.Contains('e'))
                    param |= SearchParams.Eng;
                if (prm.Contains('t'))
                    param |= SearchParams.Trans;

                return Search(expr.Substring(i + 1), param);
            }

            return Search(expr, SearchParams.EngOrTrans);

        }

        static MappedData()
        {
            namespaces.Add(string.Empty, string.Empty);
        }
    }
}
