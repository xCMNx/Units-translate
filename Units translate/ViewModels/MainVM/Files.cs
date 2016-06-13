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

        ICollection<IMapData> ExecFilterFiles(string filter = null)
        {
            ICollection<IMapData> data = MappedData.Data.Cast<IMapData>().ToArray();
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
                catch (Exception e)
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
            if (Selected != null && Selected.FullPath.Equals(name, StringComparison.InvariantCultureIgnoreCase))
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
            setWatcher(path);
            AddFiles(Directory.GetFiles(path, "*.*", SearchOption.AllDirectories).Where(f => !IsIgnored(f)).ToArray(), callback);
        }

        void AddFiles(ICollection<string> files, Action<string, int> callback = null)
        {
            EditingEnabled = false;
            try
            {
                if (callback != null) callback(null, files.Count());
                int cnt = 0;
                foreach (var f in files)
                {
                    if (callback != null) callback(f, ++cnt);
                    MappedData.AddData(new FileContainer(f), false);
                }
            }
            finally
            {
                EditingEnabled = true;
            }
        }

        public void OpenSolution(ISolutionReader reader, string file, Action<string, int> callback = null)
        {
            var files = reader.GetFiles(file);
            if (files != null)
            {
                if (MappedData.Data.Count > 0 && MessageBox.Show("Удалить уже добавленные файлы?", "Добавлеие решения", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    Helpers.mainCTX.Send(_ => MappedData.Data.Clear(), null);
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
            var pair = ExecDialog(new Microsoft.Win32.OpenFileDialog()
                , Mappers.SolutionReaders
                , LAST_SOLUTION_READER
                , LAST_SOLUTION_FILE
            );
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
                EditingEnabled = true;
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
