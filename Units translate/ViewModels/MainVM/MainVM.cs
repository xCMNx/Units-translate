using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Core;
using Microsoft.Win32;
using Ui;
using System.ComponentModel;

namespace Units_translate
{
    public partial class MainVM : CommandSink, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        public void Route(object sender, PropertyChangedEventArgs e) => PropertyChanged?.Invoke(sender, e);

        public void NotifyPropertiesChanged(params string[] propertyNames)
        {
            if (PropertyChanged != null)
            {
                foreach (var prop in propertyNames)
                    PropertyChanged(this, new PropertyChangedEventArgs(prop));
            }
        }
        #endregion

        #region UI properties
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
                FilterFiles();
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
                FilterFiles();
                NotifyPropertyChanged(nameof(CyrilicOnly));
            }
        }

        const string ONLY_LETTERS = "ONLY_LETTERS";
        /// <summary>
        /// Флаг указывающий, что нужно отображать только файлы в которых найдены строки содержащие символы,
        /// т.е. строки из цифр и пустые строки будут игнорироваться
        /// Используется при отображении файлов, и отображении списка строк файла
        /// </summary>
        public bool LetterOnly
        {
            get { return FileContainer.LetterOnly; }
            set
            {
                FileContainer.LetterOnly = value;
                FilterFiles();
                Helpers.ConfigWrite(ONLY_LETTERS, FileContainer.LetterOnly);
                NotifyPropertyChanged(nameof(LetterOnly));
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

        const string MAP_METHODS = "MAP_METHODS";
        /// <summary>
        ///Разрешает мапперу размечать методы
        /// </summary>
        public bool MapMethods
        {
            get { return FileContainer.MapMethods; }
            set
            {
                FileContainer.MapMethods = value;
                Helpers.ConfigWrite(MAP_METHODS, FileContainer.MapMethods);
                NotifyPropertyChanged(nameof(MapMethods));
            }
        }

        public const string CONSOLE_ENABLED = "CONSOLE";
        public bool ConsoleEnabled
        {
            get { return Helpers.ConsoleEnabled; }
            set
            {
                Helpers.ConfigWrite(CONSOLE_ENABLED, value);
                try
                {
                    Helpers.ConsoleEnabled = value;
                    if (Helpers.ConsoleEnabled)
                        Helpers.ConsoleWrite("Console enabled");
                }
                catch (Exception e)
                {
                    Helpers.ConsoleEnabled = false;
                    MessageBox.Show($"Ошибка запуска консоли. {e.Message}");
                }
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
                if (_EditingEnabled != value)
                {
                    _EditingEnabled = value;
                    NotifyPropertyChanged(nameof(EditingEnabled));
                }
            }
        }

        FileContainer _Selected = null;
        /// <summary>
        /// Выбранный файл
        /// </summary>
        public FileContainer Selected
        {
            get { return _Selected; }
            set
            {
                if (_Selected != value)
                {
                    _Selected = value;
                    NotifyPropertyChanged(nameof(Selected));
                }
            }
        }

        public bool EditorIsEnabled => SelectedValue != null;

        bool _EditorIsShown = false;
        public bool EditorIsShown
        {
            get { return _EditorIsShown; }
            set
            {
                if (_EditorIsShown != value)
                {
                    _EditorIsShown = value;
                    NotifyPropertiesChanged(nameof(EditorIsShown), nameof(EditorIsHidden));
                }
            }
        }

        public bool EditorIsHidden
        {
            get { return !_EditorIsShown; }
            set
            {
                if (_EditorIsShown == value)
                {
                    _EditorIsShown = !value;
                    NotifyPropertiesChanged(nameof(EditorIsShown), nameof(EditorIsHidden));
                }
            }
        }

        public bool IsSelectedValueHasAnalogs { get; protected set; }
        public ICollection<IMapValueRecord> SelectedValueAnalogs { get; protected set; }

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
                    SelectedValueAnalogs = value?.GetAnalogs<IMapValueRecord>();
                    IsSelectedValueHasAnalogs = SelectedValueAnalogs != null && SelectedValueAnalogs.Count() > 0;
                    NotifyPropertiesChanged(nameof(SelectedValue), nameof(EditorIsEnabled), nameof(IsSelectedValueHasAnalogs), nameof(SelectedValueAnalogs));
                }
            }
        }

        #endregion

        #region ShowValueQuery
        /// <summary>
        /// Подписчик получает запрос на отображение строки
        /// </summary>
        public event Action<IMapRangeItem> ShowValueQuery;

        protected void OnShowValueQuery(IMapRangeItem e)
        {
            if (ShowValueQuery != null)
                ShowValueQuery(e);
        }

        /// <summary>
        /// Просит отобразить строку подписчиков
        /// </summary>
        /// <param name="value">Строка для отображения</param>
        public void ShowValue(IMapRangeItem value) => OnShowValueQuery(value);
        #endregion

        static char[] pathDelimiter = new[] { '\\' };

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

        public static ICollection<Encoding> _Encodings = Encoding.GetEncodings().Select(ei => ei.GetEncoding()).OrderBy(e => e.HeaderName).ToArray();
        public ICollection<Encoding> Encodings => _Encodings;

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

        public static KeyValuePair<string, T>? ExecDialog<T>(FileDialog dialog, string filter, string configReaderHeader, string configFileHeader, Func<string, int> onReaderindexQuery, Func<int, T> onReaderQuery, Func<int, string> onReaderNameQuery)
        {
            dialog.Filter = filter;
            dialog.FilterIndex = onReaderindexQuery(Helpers.ReadFromConfig(configReaderHeader)) + 1;
            var fn = Helpers.ReadFromConfig(configFileHeader);
            if (!string.IsNullOrWhiteSpace(fn))
            {
                var p = Path.GetDirectoryName(fn);
                if (Directory.Exists(p))
                    dialog.InitialDirectory = p;
                dialog.FileName = Path.GetFileName(fn);
            }
            if (dialog.ShowDialog().Value == true)
            {
                Helpers.ConfigWrite(configReaderHeader, onReaderNameQuery(dialog.FilterIndex - 1));
                Helpers.ConfigWrite(configFileHeader, dialog.FileName);
                return new KeyValuePair<string, T>(dialog.FileName, onReaderQuery(dialog.FilterIndex - 1));
            }
            return null;
        }

        public static KeyValuePair<string, T>? ExecDialog<T, T2>(FileDialog dialog, IList<KeyValuePair<T, T2>> pairsList, string configReaderHeader, string configFileHeader) where T2 : IRepresentationAttribute
        {
            return ExecDialog(dialog
                , pairsList.Filter()
                , configReaderHeader
                , configFileHeader
                , pairsList.IndexOfReader
                , pairsList.GetReader
                , pairsList.GetReaderName
            );
        }

        #region Commands

        private static RoutedUICommand _Update;
        public static RoutedUICommand CmdUpdate => _Update;
        public Command ShowInExplorer { get; } = new Command(filePath =>
        {
            if (File.Exists((string)filePath))
                System.Diagnostics.Process.Start("explorer.exe", @"/select, " + filePath);
        });

        public Command OpenFile { get; } = new Command(filePath =>
        {
            try
            {
                System.Diagnostics.Process.Start((string)filePath);
            } catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        });

        public Command SaveTranslates { get; } = new Command(param =>
        {
            var pair = ExecSaveTranslates();
            if (pair.HasValue)
                Instance.SaveTranslations(pair.Value.Key, pair.Value.Value);
        });

        public Command OpenTranslates { get; } = new Command(param =>
        {
            var pair = ExecOpenTranslates();
            if (pair.HasValue)
                Instance.LoadTranslations(pair.Value.Key, pair.Value.Value);
        });

        public Command UpdateTranslationsSet { get; } = new Command(param => Instance.UpdateTranslatesEntries());

        public Command SaveTranslatesNew { get; } = new Command(param =>
        {
            var pair = ExecSaveTranslates();
            if (pair.HasValue)
                Instance.SaveTranslationsNew(pair.Value.Key, pair.Value.Value);
        });

        public void CleanTranslations(Func<IMapValueRecord, bool> isInvalid)
        {
            var cnt = _Translations.Count;
            for (int i = _Translations.Count - 1; i >= 0; i--)
                if (isInvalid(_Translations[i]))
                    _Translations.RemoveAt(i);
            if (cnt != _Translations.Count)
                MessageBox.Show(string.Format("Удалено {0}.", cnt - _Translations.Count));
        }

        public Command RemoveValuesWithoutTranslation { get; } = new Command(param => Instance.CleanTranslations(itm => string.IsNullOrWhiteSpace(itm.Translation)));

        public Command RemoveTranslationsWithoutBinding { get; } = new Command(param => Instance.CleanTranslations(itm => itm.Data.Count == 0));

        public Command RemoveUnused { get; } = new Command(param => Instance.CleanTranslations(itm => itm.Data.Count == 0 && !Core.Translations.IsValueOriginal(itm.Value)));

        public Command RemoveUnlinked { get; } = new Command(param => Instance.CleanTranslations(itm => itm.Data.Count == 0 && itm.WasLinked));

        //public Command OpenFile { get; } = new Command(param => );
        //public Command OpenFile { get; } = new Command(param => );
        //public Command OpenFile { get; } = new Command(param => );

        static void _InitCommands()
        {
            InputGestureCollection inputs = new InputGestureCollection();
            inputs.Add(new KeyGesture(Key.F5, ModifierKeys.None, "F5"));
            _Update = new RoutedUICommand("Update", "Update", typeof(MainVM), inputs);
        }
        #endregion

        #region Constructor
        public MainVM()
        {
            if (_Instance != null)
                throw new Exception("MainVM already exists");
            _Instance = this;
            initTranslation();
            UnitsCommands();
        }

        static MainVM()
        {
            Helpers.ConsoleBufferHeight = 1000;
            Helpers.ConsoleEnabled = Helpers.ConfigRead(CONSOLE_ENABLED, Helpers.ConsoleEnabled);
            Core.Search.CaseInsensitiveSearch = Helpers.ConfigRead(CASE_INSANSITIVE_SEARCH, Core.Search.CaseInsensitiveSearch, true);
            FileContainer.LetterOnly = Helpers.ConfigRead(ONLY_LETTERS, FileContainer.LetterOnly);
            FileContainer.ShowMappingErrors = Helpers.ConfigRead(SHOW_MAPING_ERRORS, FileContainer.ShowMappingErrors);
            FileContainer.MapMethods = Helpers.ConfigRead(MAP_METHODS, FileContainer.MapMethods);
            FileContainer.FixingEnabled = Helpers.ConfigRead(FIXING_ENABLED, FileContainer.FixingEnabled);
            _InitCommands();
        }

        public static MainVM Instance => _Instance;
        static MainVM _Instance = null;
        #endregion
    }
}
