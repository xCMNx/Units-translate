using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using Core;
using Ui;

namespace Units_translate
{
    public class FileContainer : PathBase, IPathNode, IMapData
    {
        public PathPart Parent { get; protected set; }

        public override string Path => Parent.FullPath;
        int _StringsCount = 0;
        int _CyrilicCount = 0;
        bool _ContainsLiteral = false;
        public override int StringsCount => _StringsCount;
        public override int CyrilicCount => _CyrilicCount;
        public override string[] FullPathParts => Parent.FullPathParts.Concat(new string[] { Name }).ToArray();
        public bool _Visible = true;
        public bool Visible
        {
            get { return _Visible; }
            set
            {
                if (_Visible != value)
                {
                    _Visible = value;
                    if (!IsUpdating)
                        NotifyPropertiesChanged(nameof(IsVisible), nameof(Visible));
                }
            }
        }
        public override bool IsVisible => _Visible;

        public string Ext => System.IO.Path.GetExtension(Name);

        //регулярка для проверки наличия кирилицы в строке
        static Regex cyrRegex = new Regex(@"\p{IsCyrillic}|\p{IsCyrillicSupplement}");
        //переключалка отображения только строк с символами
        public static bool LetterOnly = false;
        //Разрешает фиксить файлы мапперу
        public static bool FixingEnabled = false;
        //Разрешает мапперу размечать методы
        public static bool MapMethods = false;

        #region IMapData
        List<IMapRangeItem> _Items;
        public ICollection<IMapRangeItem> Items => _Items;
        public bool IsMapped  => _Items != null;
        #endregion

        public const string WRITE_ENCODING = "WRITE_ENCODING";
        public const string WRITE_ENCODING_ACTIVE = "WRITE_ENCODING_ACTIVE";
        public static Encoding WriteEncoding = Encoding.GetEncoding(Helpers.ReadFromConfig(WRITE_ENCODING, Helpers.Default_Encoding));
        public static bool UseWriteEncoding = Helpers.ConfigRead(WRITE_ENCODING_ACTIVE, false);

        public ICollection<IMapRangeItem> ShowingItems => Items?.Where(it => it is IMapValueItem && (!LetterOnly || (it as IMapValueItem).Value.ContainsLetter())).ToArray();

        /// <summary>
        /// Есть ли в файле строки с символами
        /// </summary>
        public bool ContainsLiteral() => _ContainsLiteral;

        public string Text
        {
            get
            {
                //костыль, т.к. при изменении файла приходит сообщение, и мы просим текст для разметки, но сам файл еще санят
                //делаем несколько попыток получения текста
                int i = 0;
                while (i < 3)
                    try
                    {
                        string path = FullPath;
                        return System.IO.File.Exists(path) ? File.ReadAllText(path, Helpers.GetEncoding(path, Helpers.Encoding)) : string.Empty;
                    }
                    catch (Exception e)
                    {
                        Helpers.ConsoleWrite(e.ToString(), ConsoleColor.Red);
                        i++;
                        Thread.Sleep(100);
                    }
                return string.Empty;
            }
        }

        /// <summary>
        /// Сохраняем новый текст файла
        /// </summary>
        /// <param name="text">Новый текст</param>
        public void SaveText(string text)
        {
            var path = FullPath;
            var encoding = UseWriteEncoding ? WriteEncoding : Helpers.GetEncoding(path, Helpers.Encoding);
            System.IO.File.WriteAllText(path, text, encoding);
            MappedData.UpdateData(this, true);
        }

        /// <summary>
        /// Возвращает области разметки попадаюзие в диапазон
        /// </summary>
        /// <param name="start">Начало диапазона поиска</param>
        /// <param name="end">Конец диапазона поиска</param>
        /// <returns></returns>
        public ICollection<T> ItemsBetween<T>(int start, int end) where T : class, IMapRangeItem
        {
            var res = new List<T>();
            foreach (var item in _Items)
            {
                if (item.Start > end)
                    break;
                if (item.End > start && item as T != null)
                    res.Add(item as T);
            }
            return res;
        }

        /// <summary>
        /// последняя дата изменения файла
        /// </summary>
        public DateTime LastUpdate { get; private set; } = DateTime.MinValue;

        public bool IsChanged => File.GetLastWriteTime(FullPath) != LastUpdate;

        public static bool ShowMappingErrors = true;

        /// <summary>
        /// Просит переразметить файл
        /// </summary>
        /// <param name="ifChanged">Только изменившийся</param>
        /// <param name="safe">Нужна ли синхронизация</param>
        public void Remap(bool ifChanged)
        {
            var path = FullPath;
            var lastwrite = File.GetLastWriteTime(path);
            if (ifChanged && LastUpdate == lastwrite)
                return;
            LastUpdate = lastwrite;
            try
            {
                _Items = null;
                if (!System.IO.File.Exists(path))
                    return;
                var mapper = Core.Mappers.FindMapper(Ext);
                if (mapper != null)
                    try
                    {
                        Action tryGet = () =>
                        {
                            _Items = mapper.GetMap(Text, Ext, MapMethods ? MapperOptions.MapMethods : MapperOptions.None).OrderBy(it => it.Start).ToList();
                        };
                        try
                        {
                            tryGet();
                        }
                        catch (MapperFixableException e)
                        {
                            if (!FixingEnabled)
                                throw;
                            //попытка пофиксить
                            Helpers.ConsoleWrite($"{path}\r\n{e.ToString()}", ConsoleColor.DarkRed);
                            mapper.TryFix(path, Helpers.GetEncoding(path, Helpers.Encoding));
                            tryGet();
                            if (ShowMappingErrors)
                                MessageBox.Show(string.Format("Произошла ошибка во время обработки файла, файл был исправлен:\r\n{0}\r\n\r\n{1}", path, e));
                        }
                    }
                    catch (Exception e)
                    {
                        Helpers.ConsoleWrite($"{path}\r\n{e.ToString()}", ConsoleColor.Red);
                        _Items = new List<IMapRangeItem>();
                        if (ShowMappingErrors)
                            MessageBox.Show(string.Format("Произошла ошибка во время обработки файла:\r\n{0}\r\n\r\n{1}", path, e));
                    }
            }
            finally
            {
                if (Items == null)
                {
                    _StringsCount = _CyrilicCount = 0;
                    _ContainsLiteral = false;
                }
                else
                {
                    var mapValItems = Items.OfType<IMapValueItem>();
                    _StringsCount = mapValItems.Count();
                    _CyrilicCount = mapValItems.Where(it => cyrRegex.IsMatch(it.Value)).Count();
                    _ContainsLiteral = mapValItems.Any(it => it.Value.Any(c => char.IsLetter(c)));
                }
                if (!IsUpdating)
                    NotifyPropertiesChanged(nameof(CyrilicCount), nameof(StringsCount), nameof(Items), nameof(ShowingItems));
            }
        }

        /// <summary>
        /// Возвращает область разметки являющуюся значением по указанному смещению
        /// </summary>
        /// <param name="offset">Смещение в тексте</param>
        /// <returns>Найденная область или null</returns>
        public IMapValueItem ValueItemAt(int offset)
        {
            foreach (var item in _Items)
                if (item.Start > offset)
                    break;
                else if (item is IMapValueItem && item.Start <= offset && item.End >= offset)
                    return item as IMapValueItem;
            return null;
        }

        /// <summary>
        /// Возвращает области разметки по указанному смещению
        /// </summary>
        /// <param name="offset">Смещение в тексте</param>
        /// <returns>Найденные области</returns>
        public ICollection<T> ItemsAt<T>(int offset) where T : IMapRangeItem
        {
            var res = new List<T>();
            foreach (var item in _Items)
            {
                if (item.Start > offset)
                    break;
                else if (item.Start <= offset && item.End >= offset && item is T)
                    res.Add((T)item);
            }
            return res;
        }

        /// <summary>
        /// Вернет список разметок значения которых является переданный объект. Разметки сами должны сверять себя с объектами.
        /// </summary>
        /// <param name="obj">Искомый объект</param>
        /// <returns></returns>
        public ICollection<T> GetItemsWithValue<T>(object obj) where T : IMapBaseItem
        {
            var lst = new List<T>();
            if (obj != null)
                foreach (var itm in Items)
                {
                    if (itm is T && ((T)itm).IsSameValue(obj))
                        lst.Add((T)itm);
                }
            return lst;
        }

        /// <summary>
        /// Вернет количество разметок значения которых является переданный объект. Разметки сами должны сверять себя с объектами.
        /// </summary>
        /// <param name="obj">Искомый объект</param>
        /// <returns></returns>
        public int GetItemsCountWithValue(object obj)
        {
            int cnt = 0;
            if (obj != null)
                foreach (var itm in Items)
                {
                    var it = itm as IMapBaseItem;
                    if (it != null && it.IsSameValue(obj))
                        cnt++;
                }
            return cnt;
        }

        public void UnMap()
        {
            MappedData.RemoveData(this);
        }

        public override void Dispose()
        {
            UnMap();
            Parent.Remove(this);
            base.Dispose();
        }

        public bool Equals(IMapData other) => CompareTo(other as IPathBase) == 0;

        public void DoShowInExplorer() => System.Diagnostics.Process.Start("explorer.exe", @"/select, " + FullPath);
        public void DoOpenFile() => System.Diagnostics.Process.Start(FullPath);

        public static Command ShowInExplorer { get; } = new Command(f => (f as FileContainer).DoShowInExplorer());
        public static Command OpenFile { get; } = new Command(f => (f as FileContainer).DoOpenFile());

        public FileContainer(PathPart parent, string name) : base(name)
        {
            Parent = parent;
            Parent.Add(this);
            if (this != MappedData.AddData(this))
                throw new Exception("Duplicated.");
        }
    }
}
