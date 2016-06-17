using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Core;

namespace Units_translate
{
    public class PathPart : PathBase, ICollection<PathBase>, IList<PathBase>, IReadOnlyList<PathBase>, IReadOnlyCollection<PathBase>, INotifyCollectionChanged
    {
        public override int StringsCount => Childs.Sum(c => c.StringsCount);
        public override int CyrilicCount => Childs.Sum(c => c.CyrilicCount);
        protected SortedObservableCollection<PathBase> Childs = new SortedObservableCollection<PathBase>() { Comparer = PathNameComparer.Comparer };

        public PathBase this[int index] => Childs[index];

        public void Add(PathBase child)
        {
            if (Childs.IndexOf(child) < 0)
            {
                Childs.Add(child);
                child.PropertyChanged += Child_PropertyChanged;
            }
            else
                throw new System.Exception("Path allready exists.");
        }

        public IEnumerable<FileContainer> Files => Childs.SelectMany(p => p is FileContainer ? new IPathBase[] { p } : (p as PathPart).Files.OfType<IPathBase>()).OfType<FileContainer>();

        private void Child_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) => NotifyPropertyChanged(e.PropertyName);

        public bool Remove(PathBase child)
        {
            Childs.Remove(child);
            child.PropertyChanged -= Child_PropertyChanged;
            return true;
        }

        public bool Find(string path, ref PathBase last) => Find(path.PathParts(), ref last);
        public bool Find(IEnumerable<string> pathParts, ref PathBase last)
        {
            var fp = pathParts.First();
            var path = Childs.GetSorted(p => string.Compare(fp, p.Name, StringComparison.OrdinalIgnoreCase));
            if (path == null)
                return false;
            last = path;
            return pathParts.Count() == 1 || (path is PathPart && (path as PathPart).Find(pathParts.Skip(1), ref last));
        }

        public FileContainer GetFile(string name) => Childs.GetSorted(p => string.Compare(name, p.Name, StringComparison.OrdinalIgnoreCase)) as FileContainer;

        //public FileContainer AddFile(string filename) => GetFile(filename) ?? new FileContainer(this, filename);

        public override void Dispose()
        {
            Clear();
            base.Dispose();
        }

        public void Clear()
        {
            foreach (PathBase c in Childs)
                c.Dispose();
        }

        #region ICollection
        public int Count => Childs.Count;
        public bool IsReadOnly => true;

        public void Add(DriveContainer item)
        {
            throw new NotImplementedException();
        }

        public bool Contains(PathBase item) => Childs.Contains(item);

        public void CopyTo(PathBase[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<PathBase> GetEnumerator() => Childs.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Childs.GetEnumerator();

        #endregion;

        #region INotifyCollectionChanged
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        void OnCollectionChanged(NotifyCollectionChangedEventArgs args) => Helpers.mainCTX.Post(_ => CollectionChanged?.Invoke(this, args), null);
        private void Childs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => OnCollectionChanged(e);
        #endregion

        #region IList
        PathBase IList<PathBase>.this[int index]
        {
            get
            {
                return Childs[index];
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public int IndexOf(PathBase item) => Childs.IndexOf(item);
        public void Insert(int index, PathBase item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }
        #endregion

        public PathPart(string name) : base(name)
        {
            Childs.CollectionChanged += Childs_CollectionChanged;
            //Representer.ItemsSource = this;
        }
    }
}
