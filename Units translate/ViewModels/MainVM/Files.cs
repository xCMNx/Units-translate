using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using Core;
using Ui;

namespace Units_translate
{
    public partial class MainVM
    {
        public FilesList FilesTree { get; } = new FilesList();
        ObservableCollectionEx<TreeListViewItem> _Files = new ObservableCollectionEx<TreeListViewItem>();

        #region FilesFilter
        const string CASE_INSANSITIVE_FILE_SEARCH = "CASE_INSANSITIVE_FILE_SEARCH";
        public bool _CaseInsensitiveFileSearch = true;
        /// <summary>
        /// Делает поиск не чуствительным к регистру символов
        /// </summary>
        public bool CaseInsensitiveFileSearch
        {
            get { return _CaseInsensitiveFileSearch; }
            set
            {
                if (_CaseInsensitiveFileSearch != value)
                {
                    _CaseInsensitiveFileSearch = value;
                    Helpers.ConfigWrite(CASE_INSANSITIVE_FILE_SEARCH, _CaseInsensitiveFileSearch);
                    NotifyPropertyChanged(nameof(CaseInsensitiveFileSearch));
                }
            }
        }

        string ActiveFilter = null;
        public string FileFilter
        {
            set
            {
                ActiveFilter = value;
                FilterFiles();
            }
        }

        public void FilterFiles()
        {
            FilesTree.BeginUpdate();
            try
            {
                ICollection<FileContainer> data = MappedData.Data.OfType<FileContainer>().ToArray();
                try
                {
                    var expr = string.IsNullOrWhiteSpace(ActiveFilter) ? null : new Regex(ActiveFilter, _CaseInsensitiveFileSearch ? RegexOptions.IgnoreCase : RegexOptions.None, new TimeSpan(0, 0, 1));
                    foreach (var f in data)
                        f.Visible = (!_MappedOnly || f.IsMapped)
                            && (!_CyrilicOnly || f.CyrilicCount > 0)
                            && (!LetterOnly || f.ContainsLiteral())
                            && (expr == null || expr.IsMatch(f.Name));
                }
                catch (Exception e)
                {
                    Helpers.ConsoleWrite(e.ToString(), ConsoleColor.Blue);
                }
            }
            finally
            {
                FilesTree.EndUpdate();
            }
        }
        #endregion

        /// <summary>
        /// Добавляем файлы в каталоге и его подкаталогах
        /// </summary>
        /// <param name="path">Путь к каталогу</param>
        /// <param name="callback">Колбэк метод (путь, индекс), сначало вернет пустой путь и количество файлов, затем путь и индес файла</param>
        public void AddDir(string path, Action<string, int> callback = null)
        {
            setWatcher(path);
            AddFiles(Directory.GetFiles(path, "*.*", SearchOption.AllDirectories).Where(f => !IsIgnored(f)).ToArray(), callback);
        }

        void AddFiles(ICollection<string> files, Action<string, int> callback = null)
        {
            EditingEnabled = false;
            FilesTree.BeginUpdate();
            try
            {
                callback?.Invoke(null, files.Count);
                int cnt = 0;
                //System.Diagnostics.Stopwatch w = new System.Diagnostics.Stopwatch();
                //w.Start();
                foreach (var f in files)
                {
                    callback?.Invoke(f, ++cnt);
                    FilesTree.AddFile(f);
                }
                //w.Stop();
            }
            finally
            {
                FilesTree.EndUpdate();
                EditingEnabled = true;
                FilterFiles();
                GC.Collect();
            }
        }

        public void OpenSolution(ISolutionReader reader, string file, Action<string, int> callback = null)
        {
            var files = reader.GetFiles(file);
            if (files != null)
            {
                if (MappedData.Data.Count() > 0 && MessageBox.Show("Удалить уже добавленные файлы?", "Добавлеие решения", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    Helpers.mainCTX.Send(_ => MappedData.Clear(), null);
                    foreach (var w in watchers)
                        w.Value.EnableRaisingEvents = false;
                    watchers.Clear();
                }
                foreach (var d in new HashSet<string>(files.Select(f => Path.GetDirectoryName(f)), StringComparer.InvariantCultureIgnoreCase))
                    setWatcher(d);
                AddFiles(files, callback);
                foreach (var f in files)
                    _SolutionsFiles.Add(f);
            }
        }

        HashSet<string> _SolutionsFiles = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
        const string LAST_SOLUTION_READER = "LAST_SOLUTION_READER";
        const string LAST_SOLUTION_FILE = "LAST_SOLUTION_FILE";

        public void OpenSolution(Action<string, int> callback = null)
        {
            var pair = Helpers.mainCTX.Get(() => ExecDialog(new Microsoft.Win32.OpenFileDialog()
                , Mappers.SolutionReaders
                , LAST_SOLUTION_READER
                , LAST_SOLUTION_FILE
            ));
            if (pair.HasValue)
                OpenSolution(pair.Value.Value, pair.Value.Key, callback);
        }

        /// <summary>
        /// Обновляет разметку изменившихся файлов
        /// </summary>
        /// <param name="callback">Колбэк метод (путь, индекс), сначало вернет пустой путь и количество файлов, затем путь и индес файла</param>
        public void Remap(Action<string, int> callback = null)
        {
            ResetWatchers();
            EditingEnabled = false;
            try
            {
                var lst = FilesTree.Files.ToArray();
                if (callback != null) callback(null, lst.Count());
                int cnt = 0;
                foreach (var data in lst)
                {
                    MappedData.UpdateData(data, true);
                    if (callback != null) callback((data as FileContainer)?.FullPath, ++cnt);
                }
            }
            finally
            {
                EditingEnabled = true;
                FilterFiles();
                GC.Collect();
            }
        }

        #region Ignore
        List<Regex> IgnoreList = new List<Regex>(Helpers.ReadFromConfig(IGNORE_LIST, @"(?ix)\\\.|\.exe|\.dll|\.dcu|\.obj|\.$$$|\.[^\w]").Split(LinesSplitter, StringSplitOptions.RemoveEmptyEntries).Select(l => new Regex(l)).ToArray());
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
                catch (Exception e)
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
            if (_SolutionsFiles.Count > 0 && !_SolutionsFiles.Contains(path))
                return true;
            foreach (var r in IgnoreList)
                if (r.IsMatch(path))
                    return true;
            return false;
        }
        #endregion
    }
}
