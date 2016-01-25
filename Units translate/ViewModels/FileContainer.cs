using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using Core;

namespace Units_translate
{
    public class FileContainer : PathContainer, IMapData
    {
        //регулярка для проверки наличия кирилицы в строке
        static Regex cyrRegex = new Regex(@"\p{IsCyrillic}|\p{IsCyrillicSupplement}");
        //переключалка отображения только строк с символами
        public static bool LiteralOnly = false;
        //Разрешает фиксить файлы мапперу
        public static bool FixingEnabled = false;
        #region IMapData
        List<IMapItemRange> _Items;
        public IEnumerable<IMapItemRange> Items { get { return _Items; } }
        public override string Path { get { return System.IO.Path.GetDirectoryName(_Path); } }
        public override string Name { get { return System.IO.Path.GetFileNameWithoutExtension(_Path); } }
        public override string Ext { get { return System.IO.Path.GetExtension(_Path); } }
        public bool IsMapped { get { return _Items != null; } }
        #endregion
        public override int StringsCount { get { return Items == null ? 0 : Items.Where(it => it is IMapValueItem).Count(); } }
        public override int CyrilicCount { get { return Items == null ? 0 : Items.Where(it => it is IMapValueItem && cyrRegex.IsMatch((it as IMapValueItem).Value)).Count(); } }

        public static bool ContainChar(string str)
        {
            for (int i = 0; i < str.Length; i++)
                if (char.IsLetter(str[i]))
                    return true;
            return false;
        }

        public const string WRITE_ENCODING = "WRITE_ENCODING";
        public const string WRITE_ENCODING_ACTIVE = "WRITE_ENCODING_ACTIVE";
        public static Encoding WriteEncoding = Encoding.GetEncoding(Helpers.ReadFromConfig(WRITE_ENCODING, Helpers.Default_Encoding));
        public static bool UseWriteEncoding = Helpers.ConfigRead(WRITE_ENCODING_ACTIVE, false);

        public override IEnumerable<IMapItemRange> ShowingItems { get { return Items?.Where(it => it is IMapValueItem && (!LiteralOnly || ContainChar((it as IMapValueItem).Value)) ); } }

        /// <summary>
        /// Есть ли в файле строки с символами
        /// </summary>
        public bool ContainsLiteral()
        {
            if (_Items != null)
                foreach (var it in Items)
                    if (it is IMapValueItem && ContainChar((it as IMapValueItem).Value))
                        return true;
            return false;
        }

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
                        return System.IO.File.Exists(_Path) ? File.ReadAllText(_Path, Helpers.GetEncoding(FullPath, Helpers.Encoding)) : string.Empty;
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

        public IDictionary<IMapItemRange, int> CustomRanges { get; private set; }

        /// <summary>
        /// Сохраняем новый текст файла
        /// </summary>
        /// <param name="text">Новый текст</param>
        public void SaveText(string text)
        {
            var encoding = UseWriteEncoding ? WriteEncoding : Helpers.GetEncoding(FullPath, Helpers.Encoding);
            System.IO.File.WriteAllText(FullPath, text, encoding);
        }

        public FileContainer(string fullpath) : base(fullpath)
        {
        }

        /// <summary>
        /// Возвращает области разметки попадаюзие в диапазон
        /// </summary>
        /// <param name="start">Начало диапазона поиска</param>
        /// <param name="end">Конец диапазона поиска</param>
        /// <returns></returns>
        public IEnumerable<IMapItemRange> ItemsBetween(int start, int end)
        {
            var res = new List<IMapItemRange>();
            foreach (var item in _Items)
            {
                if (item.Start > end)
                    break;
                if (item.End > start)
                    res.Add(item);
            }
            return res;
        }

        /// <summary>
        /// Очищает список областей разметки
        /// </summary>
        public void ClearItems()
        {
            _Items = null;
            NotifyPropertyChanged(nameof(Items));
        }

        /// <summary>
        /// последняя дата изменения файла
        /// </summary>
        DateTime LastUpdate = DateTime.MinValue;

        public static bool ShowMappingErrors = true;

        /// <summary>
        /// Просит переразметить файл
        /// </summary>
        /// <param name="ifChanged">Только изменившийся</param>
        /// <param name="safe">Нужна ли синхронизация</param>
        public void Remap(bool ifChanged, bool safe)
        {
            var lastwrite = File.GetLastWriteTime(FullPath);
            if (ifChanged && LastUpdate == lastwrite)
                return;
            LastUpdate = lastwrite;
            try
            {
                _Items = null;
                if (!System.IO.File.Exists(_Path))
                    return;
                var mapper = Core.Mappers.FindMapper(Ext);
                if (mapper != null)
                    try
                    {
                        Action tryGet = () =>
                        {
                            _Items = new List<IMapItemRange>(mapper.GetMap(Text, Ext).OrderBy(it => it.Start));
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
                            Helpers.ConsoleWrite(e.ToString(), ConsoleColor.Red);
                            mapper.TryFix(FullPath, Helpers.GetEncoding(FullPath, Helpers.Encoding));
                            tryGet();
                            if (ShowMappingErrors)
                                MessageBox.Show(string.Format("Произошла ошибка во время обработки файла, файл был исправлен:\r\n{0}\r\n\r\n{1}", FullPath, e));
                        }
                    }
                    catch (Exception e)
                    {
                        Helpers.ConsoleWrite(e.ToString(), ConsoleColor.Red);
                        _Items = new List<IMapItemRange>();
                        if (ShowMappingErrors)
                            MessageBox.Show(string.Format("Произошла ошибка во время обработки файла:\r\n{0}\r\n\r\n{1}", FullPath, e));
                    }
            }
            finally
            {
                if (safe)
                    NotifyPropertyChanged(nameof(Items));
                else
                    Helpers.mainCTX.Send(_=> NotifyPropertyChanged(nameof(Items)), null);
            }
        }

        /// <summary>
        /// Возвращает область разметки по указанному смещению
        /// </summary>
        /// <param name="index">Смещение в тексте</param>
        /// <returns>Найденная область иди null</returns>
        public IMapItemRange ItemAt(int index)
        {
            foreach (var item in _Items)
                if (item.Start > index)
                    break;
                else if (item.Start <= index && item.End >= index)
                    return item;
            return null;
        }
    }
}
