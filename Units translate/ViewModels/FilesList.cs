using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Core;

namespace Units_translate
{
    public class FilesList : BindableBase, ICollection<DriveContainer>, IList<DriveContainer>, IReadOnlyList<DriveContainer>, IReadOnlyCollection<DriveContainer>, INotifyCollectionChanged
    {
        SortedObservableCollection<DriveContainer> Drives = new SortedObservableCollection<DriveContainer>() { Comparer = PathNameComparer.Comparer };

        public DriveContainer this[int index] => Drives[index];
        public IEnumerable<FileContainer> Files => Drives.SelectMany(d => d.Files);

        public bool Find(string path, out PathBase last) => Find(path.PathParts(), out last);
        public bool Find(IEnumerable<string> pathParts, out PathBase last)
        {
            var fd = pathParts.First();
            var drive = Drives.GetSorted(d => string.Compare(fd, d.Name, true));
            last = drive;
            return last != null && (pathParts.Count() == 1 || drive.Find(pathParts.Skip(1), ref last));
        }

        PathPart Extend(PathPart path, string ToPath) => Extend(path, ToPath.PathParts());

        PathPart Extend(PathPart path, IList<string> toPathParts) => AddNodes(path, path == null ? toPathParts : toPathParts.Skip(path.FullPathParts.Length).ToArray());

        PathPart AddNodes(PathPart current, IList<string> pathParts)
        {
            var res = current;
            var i = 0;
            if (current == null)
                res = Drives[Drives.Add(new DriveContainer(pathParts[i++]))];
            for (; i < pathParts.Count; i++)
                res = new DirectoryContainer(res, pathParts[i]);
            return res;
        }

        public PathPart AddDirectories(string path) => AddDirectories(path.PathParts());

        public PathPart AddDirectories(IList<string> pathParts)
        {
            PathBase res;
            if (!Find(pathParts, out res))
                return Extend(res as PathPart, pathParts);
            return res as PathPart;
        }

        public FileContainer AddFile(string path)
        {
            if (System.IO.File.Exists(path))
            {
                var parts = path.PathParts();
                var dir = AddDirectories(parts.Take(parts.Length - 1).ToArray());
                var filename = parts.Last();
                return dir.GetFile(filename) ?? new FileContainer(dir, filename);
            }
            return null;
        }

        public void Remove(string path)
        {
            PathBase p;
            if (Find(path, out p))
                p.Dispose();
        }

        public void UpdateData(string path, bool safe, bool ifChanged)
        {
            PathBase p;
            if (Find(path, out p))
                MappedData.UpdateData(p as FileContainer, safe, ifChanged);
        }

        public void Clear()
        {
            foreach (var d in Drives)
                (d as DriveContainer).Dispose();
            Drives.Clear();
        }

        #region ICollection
        public int Count => Drives.Count;
        public bool IsReadOnly => true;

        public void Add(DriveContainer item)
        {
            throw new NotImplementedException();
        }

        public bool Contains(DriveContainer item) => Drives.Contains(item);

        public void CopyTo(DriveContainer[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(DriveContainer item)
        {
            item.Dispose();
            return true;
        }

        public IEnumerator<DriveContainer> GetEnumerator() => Drives.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Drives.GetEnumerator();

        #endregion;

        #region INotifyCollectionChanged
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        void OnCollectionChanged(NotifyCollectionChangedEventArgs args) => Helpers.mainCTX.Post(_=> CollectionChanged?.Invoke(this, args), null);
        private void Drives_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => OnCollectionChanged(e);
        #endregion

        #region IList
        public int IndexOf(DriveContainer item) => Drives.IndexOf(item);
        public void Insert(int index, DriveContainer item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        DriveContainer IList<DriveContainer>.this[int index]
        {
            get
            {
                return Drives[index];
            }

            set
            {
                throw new NotImplementedException();
            }
        }
        #endregion

        public FilesList()
        {
            Drives.CollectionChanged += Drives_CollectionChanged;
        }

    }
}
