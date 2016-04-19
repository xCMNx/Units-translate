using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Booru.Ui;
using Core;
using UI;

namespace Units_translate
{
    public class MainVM : BindableBase
    {
        #region UI properties
        ObservableCollectionEx<TreeListViewItem> _Files = new ObservableCollectionEx<TreeListViewItem>();
        /// <summary>
        /// массив с корнями узлов списка файлов
        /// </summary>
        public ObservableCollectionEx<TreeListViewItem> Files => _Files;

        #region Settings
        const string PREVIEW_LINSES_CNT = "PREVIEW_LINSES_CNT";
        byte _PreviewLines = Helpers.ConfigRead(PREVIEW_LINSES_CNT, (byte)10);
        /// <summary>
        /// Количество строк отображаемых в предпросмотре изменений
        /// </summary>
        public byte PreviewLines
        {
            get { return _PreviewLines; }
            set
            {
                _PreviewLines = value;
                Helpers.ConfigWrite(PREVIEW_LINSES_CNT, _PreviewLines);
                NotifyPropertyChanged(nameof(PreviewLines));
            }
        }

        const string PREVIEW_EXPANDED = "PREVIEW_EXPANDED";
        bool _ExpandedPreviews = Helpers.ConfigRead(PREVIEW_EXPANDED, false);
        /// <summary>
        /// Флаг заставляющий отображать развернутые узлы в предпросмотре
        /// </summary>
        public bool ExpandedPreviews
        {
            get { return _ExpandedPreviews; }
            set
            {
                _ExpandedPreviews = value;
                Helpers.ConfigWrite(PREVIEW_EXPANDED, _ExpandedPreviews);
                NotifyPropertyChanged(nameof(ExpandedPreviews));
            }
        }

        const string ONLY_MAPPED = "ONLY_MAPPED";
        bool _MappedOnly = Helpers.ConfigRead(ONLY_MAPPED, true);
        /// <summary>
        /// Флаг заставляющий отображать только размеченные файлы
        /// </summary>
        public bool MappedOnly
        {
            get { return _MappedOnly; }
            set
            {
                _MappedOnly = value;
                Helpers.ConfigWrite(ONLY_MAPPED, _MappedOnly);
                ShowTree();
                NotifyPropertyChanged(nameof(MappedOnly));
            }
        }

        const string ONLY_CYRILIC = "ONLY_CYRILIC";
        bool _CyrilicOnly = Helpers.ConfigRead(ONLY_CYRILIC, true);
        /// <summary>
        /// Флаг заставляющий отображать только файлы содержащие строки с кирилицей
        /// </summary>
        public bool CyrilicOnly
        {
            get { return _CyrilicOnly; }
            set
            {
                _CyrilicOnly = value;
                Helpers.ConfigWrite(ONLY_CYRILIC, _CyrilicOnly);
                ShowTree();
                NotifyPropertyChanged(nameof(CyrilicOnly));
            }
        }

        const string ONLY_LITERAL = "ONLY_LITERAL";
        /// <summary>
        /// Флаг указывающий, что нужно отображать только файлы в которых найдены строки содержащие символы,
        /// т.е. строки из цифр и пустые строки будут игнорироваться
        /// Используется при отображении файлов, и отображении списка строк файла
        /// </summary>
        public bool LiteralOnly
        {
            get { return FileContainer.LiteralOnly; }
            set
            {
                FileContainer.LiteralOnly = value;
                ShowTree();
                Helpers.ConfigWrite(ONLY_LITERAL, FileContainer.LiteralOnly);
                NotifyPropertyChanged(nameof(LiteralOnly));
            }
        }

        const string SHOW_MAPING_ERRORS = "SHOW_MAPING_ERRORS";
        /// <summary>
        /// Флаг указывающий отображать ли ошибки разметки
        /// </summary>
        public bool ShowMappingErrors
        {
            get { return FileContainer.ShowMappingErrors; }
            set
            {
                FileContainer.ShowMappingErrors = value;
                Helpers.ConfigWrite(SHOW_MAPING_ERRORS, FileContainer.ShowMappingErrors);
                NotifyPropertyChanged(nameof(ShowMappingErrors));
            }
        }

        public const string CONSTOLE_ENABLED = "CONSOLE";
        public bool ConsoleEnabled
        {
            get { return Helpers.ConsoleEnabled; }
            set
            {
                Helpers.ConsoleEnabled = value;
                Helpers.ConfigWrite(CONSTOLE_ENABLED, Helpers.ConsoleEnabled);
                NotifyPropertyChanged(nameof(ConsoleEnabled));
            }
        }

        const string FIXING_ENABLED = "FIXING_ENABLED";
        /// <summary>
        /// Флаг указывающий может ли маппер фиксить файлы
        /// </summary>
        public bool FixingEnabled
        {
            get { return FileContainer.FixingEnabled; }
            set
            {
                FileContainer.FixingEnabled = value;
                Helpers.ConfigWrite(FIXING_ENABLED, FileContainer.FixingEnabled);
                NotifyPropertyChanged(nameof(FixingEnabled));
            }
        }

        #endregion

        bool _EditingEnabled = true;
        /// <summary>
        /// Выключает форму, используется пока добавляются файлы
        /// </summary>
        public bool EditingEnabled
        {
            get { return _EditingEnabled; }
            private set
            {
                _EditingEnabled = value;
                NotifyPropertyChanged(nameof(EditingEnabled));
            }
        }

        /// <summary>
        /// Полный список переводов
        /// </summary>
        public IEnumerable<IMapRecord> Translates => Core.MappedData.TranslatesDictionary;
        /// <summary>
        /// Список переводов которые связаны с файлами
        /// </summary>
        public IEnumerable<IMapRecord> UsedTranslates => MappedData.UsedTranslates;

        PathContainer _Selected = null;
        /// <summary>
        /// Выбранный файл
        /// </summary>
        public PathContainer Selected
        {
            get { return _Selected; }
            set
            {
                _Selected = value;
                NotifyPropertyChanged(nameof(Selected));
            }
        }

        public bool EditorIsEnabled=> SelectedValue != null;

        bool _EditorIsShown = false;
        public bool EditorIsShown
        {
            get { return _EditorIsShown; }
            set
            {
                _EditorIsShown = value;
                NotifyPropertyChanged(nameof(EditorIsShown));
            }
        }

        IMapValueRecord _SelectedValue;
        /// <summary>
        /// Выбранное значение
        /// </summary>
        public IMapValueRecord SelectedValue
        {
            get { return _SelectedValue; }
            set
            {
                if (_SelectedValue != value)
                {
                    _SelectedValue = value;
                    EditorIsShown = EditorIsEnabled;
                    NotifyPropertiesChanged(nameof(SelectedValue), nameof(EditorIsEnabled));
                }
            }
        }

        #endregion

        #region ShowValueQuery
        /// <summary>
        /// Подписчик получает запрос на отображение строки
        /// </summary>
        public event Action<IMapItemRange> ShowValueQuery;

        protected void OnShowValueQuery(IMapItemRange e)
        {
            if (ShowValueQuery != null)
                ShowValueQuery(e);
        }

        /// <summary>
        /// Просит отобразить строку подписчиков
        /// </summary>
        /// <param name="value">Строка для отображения</param>
        public void ShowValue(IMapItemRange value) => OnShowValueQuery(value);
        #endregion

        static char[] pathDelimiter = new[] { '\\' };

        #region Tree
        /// <summary>
        /// Строит дерево файлов на основе переданного списка размеченых данных
        /// </summary>
        /// <param name="data">Массив размеченых данных</param>
        void BuildTree(IEnumerable<IMapData> data)
        {
            _Files.Clear();
            if (data.Count() == 0)
                return;
            var lst = data.OrderBy(it => it.Path).ToArray();

            List<TreeListViewItem> res = new List<TreeListViewItem>();
            try
            {
                //текущий узел в дереве
                var currentNode = new PathContainer(lst.First().Path);
                res.Add(new TreeListViewItem() { Header = currentNode });
                //стэк узлов до корня дерева
                Stack<TreeListViewItem> st = new Stack<TreeListViewItem>();
                st.Push(res[0]);

                //лямбда для формирования нового узла
                //дополняет недостающие узлы пути
                Action<IMapData> add = (md) =>
                {
                    var p = md.Path.Split(pathDelimiter, StringSplitOptions.RemoveEmptyEntries);
                    var fp = currentNode.Path;
                    for (int i = fp.Split(pathDelimiter, StringSplitOptions.RemoveEmptyEntries).Length; i < p.Length; i++)
                    {
                        fp = Path.Combine(fp, p[i]);
                        currentNode = new PathContainer(fp);
                        var tmp = new TreeListViewItem() { Header = currentNode };
                        st.Peek().Items.Add(tmp);
                        st.Push(tmp);
                    }
                };

                foreach (var it in lst)
                {
                    var path = it.Path.ToUpper();
                    if (!currentNode.Path.Equals(path, StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (path.Contains(currentNode.Path.ToUpper()))
                            //путь совпадает с текущим, но нужно добавить узлы
                            add(it);
                        else
                        {
                            //путь либо короче, либо нужен новый рут
                            TreeListViewItem tmp = null;
                            //пройдемся по стеку, в поисках совпадающего пути
                            while (st.Count > 0)
                            {
                                currentNode = (PathContainer)st.Peek().Header;
                                if (path.Equals(currentNode.Path, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    //найден подходящий узел
                                    tmp = st.Pop();
                                    break;
                                }
                                else if (path.StartsWith(currentNode.Path.ToUpper()))
                                {
                                    //часть пути совпала, но нужно добавить узлы в путь
                                    add(it);
                                    tmp = st.Pop();
                                    break;
                                }
                                st.Pop();
                            }
                            if (tmp == null)
                            {
                                //стэк пуст, значит нужен новый рут
                                currentNode = new PathContainer(it.Path);
                                tmp = new TreeListViewItem() { Header = currentNode };
                                res.Add(tmp);
                            }
                            st.Push(tmp);
                        }
                    }
                    st.Peek().Items.Add(new TreeListViewItem() { Header = it });
                }
            }
            finally
            {
                _Files.Reset(res);
            }
        }

        #region FilesFilter

        public string FileFilter
        {
            set
            {
                FilterFiles(value);
            }
        }

        public void FilterFiles(string filter)
        {
            BuildTree(ExecFilterFiles(filter));
        }

        IEnumerable<IMapData> ExecFilterFiles(string filter = null)
        {
            IEnumerable<IMapData> data = MappedData.Data.Cast<IMapData>();
            if (_MappedOnly)
                data = data.Where(it => it.IsMapped).ToArray();
            if (_CyrilicOnly)
                data = data.Where(it => (it as FileContainer).CyrilicCount > 0).ToArray();
            if (LiteralOnly)
                data = data.Where(it => (it as FileContainer).ContainsLiteral()).ToArray();
            if (!string.IsNullOrWhiteSpace(filter))
            {
                try
                {
                    var expr = new Regex(filter);
                    data = data.Where(it => expr.IsMatch(it.FullPath)).ToArray();
                }
                catch(Exception e)
                {
                    Helpers.ConsoleWrite(e.ToString(), ConsoleColor.Blue);
                }
            }
            return data;
        }
        #endregion

        /// <summary>
        /// Формирует новое дерево в зависимости от расставленых флагов отображения
        /// </summary>
        public void ShowTree()
        {
            BuildTree(ExecFilterFiles());
        }

        /// <summary>
        /// Проходит по узлу и ищет совпадение пути в подузлах 
        /// </summary>
        /// <param name="node">Узел в котором происходит поиск</param>
        /// <param name="name">Путь</param>
        /// <returns>Eсли путь совпал выпилит подузел и вернет true</returns>
        bool RemoveFromNode(TreeListViewItem node, string name)
        {
            foreach (TreeListViewItem item in node.Items)
                if (((PathContainer)item.Header).FullPath.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                {
                    node.Items.Remove(item);
                    return true;
                }
            return false;
        }

        /// <summary>
        /// Проходит по дереву и удаляет узел с указанным путем
        /// </summary>
        /// <param name="name">Путь который надо выпилить</param>
        void RemoveFromFiles(string name)
        {
            if (Selected.FullPath.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                Selected = null;
            foreach (var f in _Files)
                if (RemoveFromNode(f, name))
                    return;
        }

        /// <summary>
        /// Добавляем файлы в каталоге и его подкаталогах
        /// </summary>
        /// <param name="path">Путь к каталогу</param>
        /// <param name="callback">Колбэк метод (путь, индекс), сначало вернет пустой путь и количество файлов, затем путь и индес файла</param>
        public void AddDir(string path, Action<string, int> callback = null)
        {
            EditingEnabled = false;
            try
            {
                var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories).Where(f => !IsIgnored(f)).ToArray();
                if (callback != null) callback(null, files.Count());
                int cnt = 0;
                foreach (var f in files)
                {
                    if (callback != null) callback(f, ++cnt);
                    MappedData.AddData(new FileContainer(f), false);
                }
                setWatcher(path);
            }
            finally
            {
                NotifyPropertyChanged(nameof(UsedTranslates));
                EditingEnabled = true;
            }
        }

        /// <summary>
        /// Обновляет разметку изменившихся файлов
        /// </summary>
        /// <param name="callback">Колбэк метод (путь, индекс), сначало вернет пустой путь и количество файлов, затем путь и индес файла</param>
        public void Remap(Action<string, int> callback = null)
        {
            EditingEnabled = false;
            try
            {
                if (callback != null) callback(null, MappedData.Data.Count);
                int cnt = 0;
                foreach (var data in MappedData.Data)
                {
                    MappedData.UpdateData((IMapData)data, false, true);
                    if (callback != null) callback(data.FullPath, ++cnt);
                }
            }
            finally
            {
                NotifyPropertyChanged(nameof(UsedTranslates));
                EditingEnabled = true;
            }
        }


        #endregion

        #region Files monitoring
        /// <summary>
        /// список системных мониторов за изменениями папок и файлов в них
        /// </summary>
        Dictionary<string, FileSystemWatcher> watchers = new Dictionary<string, FileSystemWatcher>();

        /// <summary>
        /// добавляеи папку для мониторинга
        /// </summary>
        /// <param name="path"></param>
        void setWatcher(string path)
        {
            path = path.ToUpper();
            if (watchers.ContainsKey(path))
                return;
            var w = new FileSystemWatcher(path);
            w.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            w.Filter = "*.*";
            w.Changed += W_Changed;
            w.Deleted += W_Deleted;
            try
            {
                //пройдем по имеющимся мониторам и подкорректируем 
                foreach (var wtc in watchers)
                    if (wtc.Key.StartsWith(path))
                        return;//новый путь входит в подкаталоги уже наблюдаемой папки, активация монитора не нужна
                    else if (path.StartsWith(wtc.Key))
                        wtc.Value.EnableRaisingEvents = false;//новая папка содержит наблюдаемый каталог, выключим наблюдение, т.к. за старой папкой будет наблюдать новый монитор
            }
            finally
            {
                watchers.Add(path, w);
            }
            //активируем наблюдение
            w.EnableRaisingEvents = true;
        }

        /// <summary>
        /// происходит при изменении в папке или файле, вызывается асинхронно
        /// </summary>
        private void W_Changed(object sender, FileSystemEventArgs e)
        {
            Helpers.mainCTX.Send(_ =>
            {
                //попросим обновить файл, и т.к. событие может произойти несколько раз, установим флаг проверки даты изменения
                Helpers.ConsoleWrite(string.Format("[{0}]{1} : {2}", DateTime.Now.ToString(), e.FullPath, e.ChangeType));
                MappedData.UpdateData(e.FullPath, true, true);
                NotifyPropertyChanged(nameof(UsedTranslates));
            }, null);
        }

        /// <summary>
        /// происходит при удалении папки или файла, вызывается асинхронно
        /// </summary>
        private void W_Deleted(object sender, FileSystemEventArgs e)
        {
            Helpers.mainCTX.Send(_ =>
            {
                //выпилим файл из разметки
                Helpers.ConsoleWrite(string.Format("[{0}]{1} : {2}", DateTime.Now.ToString(), e.FullPath, e.ChangeType));
                MappedData.RemoveData(e.FullPath);
                NotifyPropertyChanged(nameof(UsedTranslates));
                RemoveFromFiles(e.FullPath);
            }, null);
        }

        #endregion

        #region Search
        /// <summary>
        /// Вызывает поиск строки в словаре используя заданный текст как регулярку
        /// </summary>
        public string SearchText
        {
            set { Search(value); }
        }

        bool _Searching = false;
        public bool Searching
        {
            get { return _Searching; }
            protected set
            {
                _Searching = value;
                NotifyPropertyChanged(nameof(Searching));
            }
        }

        CancellationTokenSource _SearchCToken = new CancellationTokenSource();
        void Search(string Expr)
        {
            _SearchCToken.Cancel();
            var localToken = _SearchCToken = new CancellationTokenSource();
            Searching = true;
            Task.Factory.StartNew(() =>
            {
                var res = MappedData.Search(Expr, localToken.Token);
                Helpers.mainCTX.Send(_ =>
                {
                    SearchResults = res;
                    NotifyPropertyChanged(nameof(SearchResults));
                    if (_SearchCToken == localToken)
                        Searching = false;
                }, null);
            });
        }

        /// <summary>
        /// Результаты поиска
        /// </summary>
        public IEnumerable<IMapRecord> SearchResults { get; private set; }
        #endregion

        #region Encoding
        /// <summary>
        /// Кодировка для чтения файлов
        /// </summary>
        public Encoding ReadEncoding
        {
            get { return Helpers.Encoding; }
            set
            {
                Helpers.Encoding = value;
                Helpers.WriteToConfig(Helpers.ENCODING, value.HeaderName);
            }
        }

        /// <summary>
        /// Кодировка для записи файлов
        /// </summary>
        public Encoding WriteEncoding
        {
            get { return FileContainer.WriteEncoding; }
            set
            {
                FileContainer.WriteEncoding = value;
                Helpers.WriteToConfig(FileContainer.WRITE_ENCODING, value.HeaderName);
            }
        }

        public static IEnumerable<Encoding> _Encodings = Encoding.GetEncodings().Select(ei => ei.GetEncoding()).OrderBy(e => e.HeaderName).ToArray();
        public IEnumerable<Encoding> Encodings => _Encodings;

        public bool UseWriteEncoding
        {
            get { return FileContainer.UseWriteEncoding; }
            set
            {
                FileContainer.UseWriteEncoding = value;
                Helpers.ConfigWrite(FileContainer.WRITE_ENCODING_ACTIVE, value);
            }
        }
        #endregion

        #region Ignore
        List<Regex> IgnoreList = new List<Regex>(Helpers.ReadFromConfig(IGNORE_LIST, @"(?ix)\\\.|\.exe|\.dll|\.dcu|\.obj|\.[^\w]").Split(LinesSplitter, StringSplitOptions.RemoveEmptyEntries).Select(l => new Regex(l)).ToArray());
        static char[] LinesSplitter = new char[] { '\r', '\n' };
        static string IGNORE_LIST = "IGNORE_LIST";
        /// <summary>
        /// Используется для задания списка игнорируемых файлов и папок
        /// </summary>
        public string IgnoreText
        {
            get
            {
                return string.Join("\r\n", IgnoreList.Select(r => r.ToString()).ToArray());
            }
            set
            {
                try
                {
                    IgnoreList.Clear();
                    IgnoreList.AddRange(value.Split(LinesSplitter, StringSplitOptions.RemoveEmptyEntries).Select(l => new Regex(l)).ToArray());
                    Helpers.WriteToConfig(IGNORE_LIST, IgnoreText);
                }
                catch(Exception e)
                {
                    Helpers.ConsoleWrite(e.ToString(), ConsoleColor.Red);
                }
            }
        }

        /// <summary>
        /// Проверяет путь на вхождение в список игнора
        /// </summary>
        /// <param name="path">Путь</param>
        /// <returns>true если файл совпал с маской одного из игнора</returns>
        bool IsIgnored(string path)
        {
            foreach (var r in IgnoreList)
                if (r.IsMatch(path))
                    return true;
            return false;
        }
        #endregion

        #region Translates
        public bool HasTranslationConflicts => _TranslationConflicts.Count > 0;
        ObservableCollectionEx<IMapValueRecord> _Translations = new ObservableCollectionEx<IMapValueRecord>();
        public ObservableCollectionEx<IMapValueRecord> Translations => _Translations;
        ICommand _TranslatesSortCommand;
        public ICommand TranslatesSortCommand => _TranslatesSortCommand;

        SortedList<IMapRecordFull, SortedItems<string>> _TranslationConflicts = new SortedList<IMapRecordFull, SortedItems<string>>(MapRecordComparer.Comparer);
        public SortedList<IMapRecordFull, SortedItems<string>> TranslationConflicts => _TranslationConflicts;

        KeyValuePair<IMapRecordFull, SortedItems<string>> _SelectedConflict;
        public KeyValuePair<IMapRecordFull, SortedItems<string>> SelectedConflict
        {
            get { return _SelectedConflict; }
            set
            {
                _SelectedConflict = value;
                SelectedValue = _SelectedConflict.Key as IMapValueRecord;
                NotifyPropertyChanged(nameof(SelectedConflict));
            }
        }

        /// <summary>
        /// Удаляет вариант конфликтующего перевода из выбранного конфликта
        /// </summary>
        /// <param name="value">Значение</param>
        public void RemoveConflictVariant(string value)
        {
            if (_SelectedConflict.Value != null)
            {
                _SelectedConflict.Value.Remove(value);
                if (_SelectedConflict.Value.Count == 0)
                    _TranslationConflicts.Remove(_SelectedConflict.Key);
            }
            NotifyPropertiesChanged(nameof(TranslationConflicts), nameof(HasTranslationConflicts));
        }

        /// <summary>
        /// Очищает список конфликтующих переводов
        /// </summary>
        public void ClearTranslationConflicts()
        {
            TranslationConflicts.Clear();
            NotifyPropertiesChanged(nameof(TranslationConflicts), nameof(HasTranslationConflicts));
        }

        /// <summary>
        /// Загружает список переводов
        /// </summary>
        /// <param name="path">Путь к файлу переводов</param>
        public void LoadTranslations(string path)
        {
            if (MessageBox.Show("Очистить текущие переводы?", "Загрузка переводов", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                MappedData.ClearTranslates();
                TranslationConflicts.Clear();
            }
            MappedData.LoadTranslations(path, (itm, trans) =>
            {
                SortedItems<string> cur;
                if (_TranslationConflicts.TryGetValue(itm, out cur))
                    cur.Add(trans);
                else _TranslationConflicts[itm] = new SortedItems<string>() { trans };
            });
            NotifyPropertiesChanged(nameof(UsedTranslates), nameof(TranslationConflicts), nameof(HasTranslationConflicts));
        }

        public void SaveTranslations(string path)
        {
            try
            {
                Core.MappedData.SaveTranslations(path,
                    MessageBox.Show("Добавить переведённые, но отсутствующие в загруженном файле строки?", "Сохранение переводов", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes,
                    MessageBox.Show("Удалить переводы которые не найдены в файлах?", "Сохранение переводов", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes
                );
            }
            catch (Exception e)
            {
                Helpers.ConsoleWrite(e.ToString(), ConsoleColor.Red);
                MessageBox.Show(e.ToString());
            }
        }

        public void SaveTranslationsNew(string path)
        {
            try
            {
                Core.MappedData.SaveTranslations(path, Translations.Select(e => new Entry(e.Value, e.Translation)).ToArray());
            }
            catch (Exception e)
            {
                Helpers.ConsoleWrite(e.ToString(), ConsoleColor.Red);
                MessageBox.Show(e.ToString());
            }
        }

        public void UpdateTranslatesEntries()
        {
            Translations.Reset(MappedData.GetEntries().OrderBy(m => m.Value).ToArray());
        }
        #endregion

        #region Commands

        private static RoutedUICommand _Update;
        public static RoutedUICommand CmdUpdate => _Update;

        static void _InitCommands()
        {
            InputGestureCollection inputs = new InputGestureCollection();
            inputs.Add(new KeyGesture(Key.F5, ModifierKeys.None, "F5"));
            _Update = new RoutedUICommand("Update", "Update", typeof(MainVM), inputs);
        }
        #endregion

        #region Constructor
        private MainVM()
        {
            _TranslatesSortCommand = new Command((prp) =>
            {
                IEnumerable<IMapValueRecord> lst = Translations.ToArray();
                switch ((string)prp)
                {
                    case "Count":
                        lst = lst.OrderBy(e => e.Data.Count).ToArray();
                        break;
                    case "Value":
                        lst = lst.OrderBy(e => e.Value).ToArray();
                        break;
                    case "Translation":
                        lst = lst.OrderBy(e => e.Translation).ToArray();
                        break;
                }
                Translations.Reset(lst);
            });
        }


        static MainVM()
        {
            Helpers.ConsoleBufferHeight = 1000;
            Helpers.ConsoleEnabled = Helpers.ConfigRead(CONSTOLE_ENABLED, Helpers.ConsoleEnabled);
            FileContainer.LiteralOnly = Helpers.ConfigRead(ONLY_LITERAL, FileContainer.LiteralOnly);
            FileContainer.ShowMappingErrors = Helpers.ConfigRead(SHOW_MAPING_ERRORS, FileContainer.ShowMappingErrors);
            FileContainer.FixingEnabled = Helpers.ConfigRead(FIXING_ENABLED, FileContainer.FixingEnabled);
            _InitCommands();
        }

        public static MainVM Instance => _Instance ?? (_Instance = new MainVM());
        static MainVM _Instance = null;
        #endregion
    }
}
