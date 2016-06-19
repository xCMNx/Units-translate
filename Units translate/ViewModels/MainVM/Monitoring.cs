using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Core;

namespace Units_translate
{
    public partial class MainVM
    {
        /// <summary>
        /// список системных мониторов за изменениями папок и файлов в них
        /// </summary>
        Dictionary<string, FileSystemWatcher> watchers = new Dictionary<string, FileSystemWatcher>();

        /// <summary>
        /// Создает новый монитор
        /// </summary>
        /// <param name="path">Путь к наблюдаемой директории</param>
        /// <returns>Системный монитор файлов</returns>
        FileSystemWatcher GetNewWatcher(string path)
        {
            path = path.ToUpper();
            var w = new FileSystemWatcher(path);
            w.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
            w.IncludeSubdirectories = true;
            w.Filter = "*.*";
            w.Changed += W_Changed;
            w.Deleted += W_Deleted;
            w.Created += W_Created;
            w.Renamed += W_Renamed;
            return w;
        }

        /// <summary>
        /// Добавляет папку для мониторинга
        /// </summary>
        /// <param name="path">Путь к наблюдаемой директории</param>
        void setWatcher(string path)
        {
            path = path.ToUpper();
            if (watchers.ContainsKey(path))
                return;
            var w = GetNewWatcher(path);
            //пройдем по имеющимся мониторам и подкорректируем
            for (var i = watchers.Count - 1; i >= 0; i--)
            {
                var wtc = watchers.ElementAt(i);
                if (wtc.Key.StartsWith(path))
                {
                    //новая папка содержит наблюдаемый каталог, выключим наблюдение, т.к. за старой папкой будет наблюдать новый монитор
                    wtc.Value.EnableRaisingEvents = false;
                    wtc.Value.Dispose();
                    watchers.Remove(wtc.Key);
                }
                else if (path.StartsWith(wtc.Key))
                    return;//новый путь входит в подкаталоги уже наблюдаемой папки, активация монитора не нужна
            }
            watchers.Add(path, w);
            //активируем наблюдение
            w.EnableRaisingEvents = true;
        }

        void ResetWatchers()
        {
            var paths = watchers.Keys.ToArray();
            foreach (var w in watchers)
            {
                w.Value.EnableRaisingEvents = false;
                w.Value.Dispose();
            }
            foreach (var p in paths)
            {
                var nw = GetNewWatcher(p);
                watchers[p] = nw;
                nw.EnableRaisingEvents = true;
            }
        }

        private void W_Created(object sender, FileSystemEventArgs e)
        {
            Helpers.mainCTX.Send(_ =>
            {
                Helpers.ConsoleWrite(string.Format("[{0}]{1} : {2}", DateTime.Now.ToString(), e.FullPath, e.ChangeType));
                if (!IsIgnored(e.FullPath))
                    FilesTree.AddFile(e.FullPath);
            }, null);
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
                FilesTree.UpdateData(e.FullPath, true);
            }, null);
        }

        private void W_Renamed(object sender, RenamedEventArgs e)
        {
            Helpers.mainCTX.Send(_ =>
            {
                //попросим обновить файл, и т.к. событие может произойти несколько раз, установим флаг проверки даты изменения
                Helpers.ConsoleWrite(string.Format("[{0}]{1} => {2} : {3}", DateTime.Now.ToString(), e.OldFullPath, e.FullPath, e.ChangeType));
                if (!IsIgnored(e.OldFullPath))
                {
                    if (_SolutionsFiles.Contains(e.OldFullPath))
                    {
                        _SolutionsFiles.Remove(e.OldFullPath);
                        _SolutionsFiles.Add(e.FullPath);
                    }
                    FilesTree.Remove(e.OldFullPath);
                    FilesTree.AddFile(e.FullPath);
                }
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
                FilesTree.Remove(e.FullPath);
            }, null);
        }
    }
}
