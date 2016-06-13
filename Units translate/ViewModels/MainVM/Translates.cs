using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Core;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using Ui;

namespace Units_translate
{
    public partial class MainVM
    {
        const string DICTIONARIES_PATH = @".\dictionaries\";
        const string VAL_LANG_VALUE = "ValLang";
        const string TRANS_LANG_VALUE = "TransLang";

        /// <summary>
        /// Полный список переводов
        /// </summary>
        public ICollection<IMapRecord> Translates => Core.Translations.TranslatesDictionary;

        public bool IsTranslatesChanged => Core.Translations.IsTranslatesChanged();

        HashSet<string> _SpellCheckerLangs = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
        public HashSet<string> SpellCheckerLangs => _SpellCheckerLangs;

        public string ValLangPath => string.IsNullOrWhiteSpace(_ValLang) ? null : $"{DICTIONARIES_PATH}{_ValLang}";
        string _ValLang;
        public string ValLang
        {
            get { return _ValLang; }
            set
            {
                var old = _ValLang;
                _ValLang = value;
                Helpers.ConfigWrite(VAL_LANG_VALUE, _ValLang);
                NotifyPropertiesChanged(nameof(ValLang), nameof(ValLangPath));
                if (!string.IsNullOrWhiteSpace(old) && string.Equals(_ValLang, _TransLang, StringComparison.InvariantCultureIgnoreCase))
                    TransLang = old;
            }
        }

        public string TransLangPath => string.IsNullOrWhiteSpace(_TransLang) ? null : $"{DICTIONARIES_PATH}{_TransLang}";
        string _TransLang;
        public string TransLang
        {
            get { return _TransLang; }
            set
            {
                var old = _TransLang;
                _TransLang = value;
                Helpers.ConfigWrite(TRANS_LANG_VALUE, _TransLang);
                NotifyPropertiesChanged(nameof(TransLang), nameof(TransLangPath));
                if (!string.IsNullOrWhiteSpace(old) && string.Equals(_ValLang, _TransLang, StringComparison.InvariantCultureIgnoreCase))
                    ValLang = old;
            }
        }

        void InitLangs()
        {
            _ValLang = Helpers.ReadFromConfig(VAL_LANG_VALUE, "en_US");
            _TransLang = Helpers.ReadFromConfig(TRANS_LANG_VALUE, "ru_RU");
            _SpellCheckerLangs = new HashSet<string>(Directory.EnumerateFiles(DICTIONARIES_PATH, "*.dic", SearchOption.TopDirectoryOnly).Select(f => Path.GetFileNameWithoutExtension(f)).ToArray(), StringComparer.InvariantCultureIgnoreCase);
            if (!_SpellCheckerLangs.Contains(_ValLang))
                _ValLang = _SpellCheckerLangs.FirstOrDefault(v => v.Equals(_TransLang, StringComparison.InvariantCultureIgnoreCase));
            if (!_SpellCheckerLangs.Contains(_TransLang))
                _TransLang = _SpellCheckerLangs.FirstOrDefault(v => v.Equals(_ValLang, StringComparison.InvariantCultureIgnoreCase));
            NotifyPropertiesChanged(nameof(SpellCheckerLangs), nameof(TransLang), nameof(TransLangPath), nameof(ValLang), nameof(ValLangPath));
        }

        public static async Task<string> TranslateText(string input, string srcLanguageCode, string dstLanguageCode, bool fonetic)
        {
            try
            {
                string url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={srcLanguageCode}&tl={dstLanguageCode}&dt=t&q={Uri.EscapeDataString(input.Trim())}";
                var n1 = 0;
                var n2 = 0;
                if (fonetic)
                {
                    url += "&dt=rm";
                    n1 = 1;
                    n2 = 2;
                }
                var webClient = new WebClient();
                webClient.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2228.0 Safari/537.36");
                webClient.Encoding = System.Text.Encoding.UTF8;
                var objects = JArray.Parse(await Task<string>.Factory.StartNew(() => webClient.DownloadString(url)));
                var result = new StringBuilder();
                foreach (var o in objects)
                    if (o.HasValues)
                    {
                        result.Append(o[n1][n2].Value<string>());
                        result.Append(' ');
                    }
                return result.ToString().Trim();
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

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
        /// <param name="containerType">Тип контейнера</param>
        public void LoadTranslations(string path, Type containerType)
        {
            if (MessageBox.Show("Очистить текущие переводы?", "Загрузка переводов", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Core.Translations.Clear();
                TranslationConflicts.Clear();
            }
            Core.Translations.LoadTranslations(path, containerType, (itm, trans) =>
            {
                SortedItems<string> cur;
                if (_TranslationConflicts.TryGetValue(itm, out cur))
                    cur.Add(trans);
                else _TranslationConflicts[itm] = new SortedItems<string>() { trans };
            });
            NotifyPropertiesChanged(nameof(TranslationConflicts), nameof(HasTranslationConflicts));
        }

        public void SaveTranslations(string path, Type containerType)
        {
            try
            {
                Core.Translations.SaveTranslations(path, containerType,
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

        public void SaveTranslationsNew(string path, Type containerType)
        {
            try
            {
                Core.Translations.SaveTranslations(path, containerType, Translations.Select(e => new BaseTranslationItem(e.Value, e.Translation)).ToArray());
            }
            catch (Exception e)
            {
                Helpers.ConsoleWrite(e.ToString(), ConsoleColor.Red);
                MessageBox.Show(e.ToString());
            }
        }

        const string LAST_TRANSLATES_READER = "LAST_TRANSLATES_READER";
        const string LAST_TRANSLATES_FILE = "LAST_TRANSLATES_FILE";
        public static KeyValuePair<string, Type>? ExecTranslatesDialog(FileDialog dialog)
        {
            return ExecDialog(dialog
                , Core.Translations.List
                , LAST_TRANSLATES_READER
                , LAST_TRANSLATES_FILE
            );
        }

        public static KeyValuePair<string, Type>? ExecSaveTranslates() => ExecTranslatesDialog(new SaveFileDialog());

        public static KeyValuePair<string, Type>? ExecOpenTranslates() => ExecTranslatesDialog(new OpenFileDialog());

        public void UpdateTranslatesEntries()
        {
            Translations.Reset(Core.Translations.GetEntries().OrderBy(m => m.Value).ToArray());
        }

        private void initTranslation()
        {
            _TranslatesSortCommand = new Command((prp) =>
            {
                ICollection<IMapValueRecord> lst = Translations.ToArray();
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
            InitLangs();
        }

    }
}
