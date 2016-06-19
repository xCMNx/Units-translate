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
        public override int StringsCount => Childs.Sum(c => c.IsVisible ? c.StringsCount : 0);
        public override int CyrilicCount => Childs.Sum(c => c.IsVisible ? c.CyrilicCount : 0);
        protected SortedObservableCollection<PathBase> Childs = new SortedObservableCollection<PathBase>() { Comparer = PathNameComparer.Comparer };
        public override bool IsVisible => Childs.Any(c => c.IsVisible);


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

        private void Child_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (!IsUpdating)
            {
                NotifyPropertyChanged(e.PropertyName);
                if (e.PropertyName == nameof(IsVisible))
                    NotifyPropertiesChanged(nameof(StringsCount), nameof(CyrilicCount));
            }
        }

        public bool Remove(PathBase child)
        {
            Childs.Remove(child);
            child.PropertyChanged -= Child_PropertyChanged;
            return true;
        }

        HashSet<PathBase> AddBuff;
        HashSet<PathBase> DelBuff;

        private void Childs_CollectionChangingBuff(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    AddBuff.UnionWith(e.NewItems.OfType<PathBase>());
                    DelBuff.ExceptWith(e.NewItems.OfType<PathBase>());
                    return;
                case NotifyCollectionChangedAction.Remove:
                    DelBuff.UnionWith(e.NewItems.OfType<PathBase>());
                    AddBuff.ExceptWith(e.NewItems.OfType<PathBase>());
                    return;
                case NotifyCollectionChangedAction.Reset:
                    DelBuff = null;
                    AddBuff = null;
                    Childs.CollectionChanged -= Childs_CollectionChangingBuff;
                    return;
            }
        }

        protected override void UpdateStarted()
        {
            Childs.CollectionChanged -= Childs_CollectionChanged;
            if (CollectionChanged != null)
            {
                AddBuff = new HashSet<PathBase>();
                DelBuff = new HashSet<PathBase>();
                Childs.CollectionChanged += Childs_CollectionChangingBuff;
            }
            foreach (var c in Childs)
            {
                c.PropertyChanged -= Child_PropertyChanged;
                c.BeginUpdate();
            }
            base.UpdateStarted();
        }

        protected override void UpdateEnded()
        {
            Childs.CollectionChanged += Childs_CollectionChanged;
            Childs.CollectionChanged -= Childs_CollectionChangingBuff;

            if (CollectionChanged != null)
                Helpers.mainCTX.Send(_ =>
                {
                    if (AddBuff == null)
                        CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                    else
                    {
                        if (AddBuff.Count > 0)
                            CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, AddBuff.ToList() as IList));
                        if (DelBuff.Count > 0)
                            CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, DelBuff.ToList() as IList));
                    }
                    AddBuff = null;
                    DelBuff = null;
                }, null);

            foreach (var c in Childs)
            {
                c.EndUpdate();
                c.PropertyChanged += Child_PropertyChanged;
            }

            Helpers.mainCTX.Post(_ => NotifyPropertyChanged(), null);
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
        }
    }
}
