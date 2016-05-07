using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;

namespace Core
{
    #region Вспомогательные классы и интерфейсы
    /// <summary>
    /// Компарер умеющий сравнивать размеченные данные
    /// Сравнивает полные пути
    /// </summary>
    public class MapDataComparer : Comparer<IMapDataBase>
    {
        public override int Compare(IMapDataBase m1, IMapDataBase m2) => string.Compare(m1?.FullPath, m2?.FullPath, true);

        public static readonly MapDataComparer Comparer = new MapDataComparer();
    }

    public class EntryesComparer : Comparer<Entry>
    {
        public override int Compare(Entry e1, Entry e2) => string.Compare(e1.Eng, e2.Eng, true);

        public static readonly EntryesComparer Comparer = new EntryesComparer();
    }

    public interface IMapRecord : IComparable
    {
        string Value { get; }
    }

    public interface IMapRecordFull : IMapRecord
    {
        SortedObservableCollection<IMapData> Data { get; }
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
    public struct MapMethodRecord : IMapRecordFull
    {
        string value;
        public string Value => value;
        SortedObservableCollection<IMapData> data;
        public SortedObservableCollection<IMapData> Data => data;

        public MapMethodRecord(string val)
        {
            value = val;
            data = new SortedObservableCollection<IMapData>() { Comparer = MapDataComparer.Comparer };
        }
        public int CompareTo(object obj)
        {
            var rec = obj as IMapRecord;
            return rec == null ? -1 : Value.CompareTo(rec.Value);
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

        SortedObservableCollection<IMapData> data;
        public SortedObservableCollection<IMapData> Data => data;

        public MapValueRecord(string val, string trans)
        {
            value = val;
            translation = trans;
            data = new SortedObservableCollection<IMapData>() { Comparer = MapDataComparer.Comparer };
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

    #endregion
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
        static SortedItems<IMapRecord> _ValuesDictionary = new SortedItems<IMapRecord>() { Comparer = MapRecordComparer.Comparer };

        /// <summary>
        /// Словарь методов
        /// </summary>
        static SortedItems<IMapRecord> _MethodsDictionary = new SortedItems<IMapRecord>() { Comparer = MapRecordComparer.Comparer };

        static SortedObservableCollection<IMapRecord> _TranslatesDictionary = new SortedObservableCollection<IMapRecord>() { Comparer = MapRecordComparer.Comparer };
        /// <summary>
        /// Словарь переводов
        /// </summary>
        public static SortedObservableCollection<IMapRecord> TranslatesDictionary => _TranslatesDictionary;
        /// <summary>
        /// Словарь переводов которые связаны с файлами
        /// </summary>
        public static ICollection<IMapRecord> UsedTranslates => _TranslatesDictionary.Where(tr => ((IMapRecordFull)tr).Data.Count > 0).ToArray();

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

        public static bool IsTranslatesChanged()
        {
            var newEntr = GetEntries().ToDictionary(e => e.Value);
            return OriginalData.Entryes.Any(e =>
            {
                IMapValueRecord tr;
                return !newEntr.TryGetValue(e.Eng, out tr) || tr.Translation != e.Trans;
            });
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

        /// <summary>
        /// Очищает переводы
        /// </summary>
        public static void ClearTranslates()
        {
            _TranslatesDictionary.Clear();
            foreach (IMapValueRecord it in _ValuesDictionary)
                it.Translation = string.Empty;
        }

        static Linguist OriginalData = new Linguist();
        public static Encoding encoding = Encoding.GetEncoding(Helpers.Default_Encoding);
        static XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
        static XmlWriterSettings WriterSettings = new XmlWriterSettings { Indent = true, IndentChars = "\t", NewLineHandling = NewLineHandling.None };
        static XmlSerializer serializer = new XmlSerializer(typeof(Linguist));

        public static bool IsValueOriginal(string val)
        {
            return OriginalData.Entryes.FirstOrDefault(e => e.Eng == val) != null;
        }

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

            var data = OriginalData.Entryes.OrderBy(e => e.Eng).ToArray();
            var lst = new SortedItems<IMapRecordFull>() { Comparer = MapRecordComparer.Comparer };

            int repeatCnt = 0;
            int conflictsCnt = 0;
            foreach (var entry in data)
            {
                var item = (IMapValueRecord)GetValueRecord(entry.Eng);
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
            var lst = new List<Entry>();
            foreach (IMapValueRecord it in _TranslatesDictionary)
                if (!removeempty || it.Data.Count > 0)
                    lst.Add(new Entry(it.Value, it.Translation));
            if (addnew)
                foreach (IMapValueRecord it in _ValuesDictionary.Except(_TranslatesDictionary))
                    if (it.Data.Count > 0 && !string.IsNullOrWhiteSpace(it.Translation))
                        lst.Add(new Entry(it.Value, it.Translation));

            SaveTranslations(path, lst);
        }

        public static void SaveTranslations(string path, IEnumerable<Entry> Entries)
        {
            OriginalData.Entryes.Clear();
            OriginalData.Entryes = Entries.OrderBy(ent => ent.Eng, StringComparer.InvariantCulture).ToList();

            using (StreamWriter sw = new StreamWriter(path, false, encoding))
            using (var xw = XmlWriter.Create(sw, WriterSettings))
            {
                xw.WriteStartDocument(true);
                //xw.WriteRaw(string.Format("\r\n<?xml-stylesheet type=\"text/xsl\" href=\"{0}.xsl\"?>\r\n", Path.GetFileNameWithoutExtension(path)));
                xw.WriteRaw("\r\n<?xml-stylesheet type=\"text/xsl\" href=\"eng_rus.xsl\"?>\r\n");
                serializer.Serialize(xw, OriginalData, namespaces);
            }
        }

        /// <summary>
        /// Возвращает список значений из загружунных переводов, и дополняет его отсутствующими значениями имеющими перевод
        /// </summary>
        /// <returns></returns>
        public static HashSet<IMapValueRecord> GetEntries()
        {
            var lst = new HashSet<IMapValueRecord>();
            foreach (IMapValueRecord it in _TranslatesDictionary)
                lst.Add(it);
            foreach (IMapValueRecord it in _ValuesDictionary.Except(_TranslatesDictionary))
                if (!string.IsNullOrWhiteSpace(it.Translation))
                    lst.Add(it);
            return lst;
        }

        [Flags]
        public enum SearchParams { EngOrTrans = 0, Eng = 1, Trans = 2, Both = 3}

        /// <summary>
        /// Поиск в словаре. Использует регулярку
        /// </summary>
        /// <param name="expr">Искомое выражение</param>
        /// <param name="param">Параметры поиска</param>
        /// <returns>Список совпадающих записей словаря</returns>
        public static ICollection<IMapRecord> Search(string expr, SearchParams param, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(expr))
                return null;
            var res = new List<IMapRecord>();
            try
            {
                var rgxp = new Regex(expr);
                Func<IMapValueRecord, bool> cmpr = null;
                if (param.HasFlag(SearchParams.Eng))
                    if (param.HasFlag(SearchParams.Trans))
                        cmpr = it => rgxp.IsMatch(it.Value) && rgxp.IsMatch(it.Translation);
                    else
                        cmpr = it => rgxp.IsMatch(it.Value);
                else if (param.HasFlag(SearchParams.Trans))
                    cmpr = it => rgxp.IsMatch(it.Translation);
                else
                    cmpr = it => rgxp.IsMatch(it.Value) || rgxp.IsMatch(it.Translation);
                foreach (IMapValueRecord it in _ValuesDictionary)
                    if (ct.IsCancellationRequested)
                        return res;
                    else if (it.Data.Count > 0 && cmpr(it))
                        res.Add(it);
            }
            catch (Exception e)
            {
                Helpers.ConsoleWrite(e.ToString(), ConsoleColor.Green);
            }
            return res;
        }

        /// <summary>
        /// Фильтр по методам, возвращает список соответствующий условиям вхождения в список переданных методов.
        /// </summary>
        /// <param name="methodsFilter">Словарь методов где значение метода указывает, строка должна попадать или не попадать в область метода.</param>
        /// <param name="lst">Фильтруемый список значений.</param>
        /// <returns>Список значений прошедших фильтрацию.</returns>
        public static ICollection<IMapRecord> MethodsFilter(IDictionary<IMapRecord, bool> methodsFilter, ICollection<IMapRecord> lst, CancellationToken ct)
        {
            if (methodsFilter == null || methodsFilter.Count() == 0 || lst == null)
                return lst;

            //файлы в которых есть все нужные методы для фильтра
            ICollection<IMapData> mData = null;
            foreach (var itm in methodsFilter)
                if (itm.Value)
                {
                    var tmp = (itm.Key as IMapRecordFull).Data as ICollection<IMapData>;
                    mData = mData == null ? tmp : mData.Intersect(tmp).ToArray();
                }

            var res = new List<IMapRecord>();
            //осуществляем перебор всех значений
            foreach (IMapRecordFull r in lst)
            {
                if (ct.IsCancellationRequested)
                    return res;
                //зразу откинем файлы в которых нет методов которые должны быть по фильтру
                var fData = mData == null ? r.Data as IList<IMapData> : r.Data.Intersect(mData).ToArray();
                foreach (var d in fData)
                {
                    //флаг обозначающий, что значение прошло все фильтры
                    var found = false;
                    //получим все разметки нашего значения в файле
                    var rItems = d.GetItemsWithValue<IMapValueItem>(r);
                    //пройдемся по каждой
                    foreach (var itm in rItems)
                    {
                        //получим методы охватывающие значение 
                        var mts = d.ItemsAt<IMapMethodItem>(itm.Start);
                        //выберем из них те, что есть в фильтре
                        var mtsF = mts.Select(m => methodsFilter.FirstOrDefault(mf => m.IsSameValue(mf.Key))).Where(k => k.Key != null).ToArray();
                        found = true;
                        //посмотрим должны ли быть вхождения в методы не охватывающие значение 
                        foreach (var mf in methodsFilter.Except(mtsF))
                            found &= !mf.Value;
                        //и тоже самое в те, что охватывали
                        if (found)
                        {
                            foreach (var mf in mtsF)
                                found &= mf.Value;
                            if (found)
                                break;
                        }
                    }
                    //значение прошло фильтры
                    if (found)
                    {
                        res.Add(r);
                        break;
                    }
                }
            }
            return res;
        }

        /// <summary>
        /// Поиск в словаре. Использует регулярку
        /// </summary>
        /// <param name="expr">
        /// Искомое выражение, в начале можно указать параметры поиска в фармате и фильтры по методам #[filter]:?[params]:[expr]
        /// <para>?e:[expr]   - вырожению должна соответствовать строка</para>
        /// <para>?t:[expr]   - вырожению должен соответствовать перевод</para>
        /// <para>?et:[expr]  - вырожению должны соответствовать и строка и её перевод</para>
        /// <para>?:[expr]    - вырожению должны соответствовать или строка или перевод</para>
        /// </param>
        /// <returns>Список совпадающих записей словаря</returns>
        public static ICollection<IMapRecord> Search(string expr, CancellationToken ct)
        {
            var lexpr = expr;

            var methodsFilter = new Dictionary<IMapRecord, bool>();
            if (lexpr.StartsWith("#"))
            {
                var i = lexpr.IndexOf(':');
                if (i < 0)
                    return null;

                var prms = lexpr.Substring(1, i - 1).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in prms)
                {
                    var prm = p;
                    if (p[0] == '!')
                        prm = p.Substring(1);
                    var m = GetMethodRecord(prm);
                    if (m == null)
                    {
                        Helpers.ConsoleWrite($"Запись {prm} не найдена.");
                        return null;
                    }
                    methodsFilter[m] = p[0] != '!';
                }
                lexpr = lexpr.Substring(i + 1);
            }

            if (lexpr.StartsWith("?"))
            {
                var i = lexpr.IndexOf(':');
                if (i < 0)
                    return null;

                SearchParams param = SearchParams.EngOrTrans;
                var prm = lexpr.Substring(0, i).ToLower();
                if (prm.Contains('e'))
                    param |= SearchParams.Eng;
                if (prm.Contains('t'))
                    param |= SearchParams.Trans;

                return MethodsFilter(methodsFilter, Search(lexpr.Substring(i + 1), param, ct), ct);
            }

            return MethodsFilter(methodsFilter, Search(lexpr, SearchParams.EngOrTrans, ct), ct);
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

        static MappedData()
        {
            namespaces.Add(string.Empty, string.Empty);
        }
    }
}
