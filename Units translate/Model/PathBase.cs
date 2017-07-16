using System;
using System.Collections.Generic;
using Core;

namespace Units_translate
{
    public class PathBaseComparer : Comparer<IPathBase>
    {
        public override int Compare(IPathBase x, IPathBase y) => string.Compare(x.FullPath, y.FullPath, StringComparison.OrdinalIgnoreCase);
        public static PathBaseComparer Comparer = new PathBaseComparer();
    }

    public class PathNameComparer : Comparer<IPathBase>
    {
        public override int Compare(IPathBase x, IPathBase y) => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
        public static PathNameComparer Comparer = new PathNameComparer();
    }

    public class PathBase : BindableBase, IDisposable, IUpdateLockable, IPathBase, IComparable<IPathBase>, IEquatable<IPathBase>, IEquatable<string>
    {
        protected string _Name;
        public virtual string Path => string.Empty;
        public virtual string FullPath => System.IO.Path.Combine(Path, Name);
        public virtual string Name => _Name;
        public virtual string Ext => string.Empty;
        public virtual int StringsCount => 0;
        public virtual int CyrilicCount => 0;
        public virtual string[] FullPathParts => new string[]{Name};
        public override string ToString() => FullPath;
        public virtual bool IsVisible => true;

        public virtual void Dispose()
        {
        }

        public int CompareTo(string obj) => string.Compare(FullPath, obj, StringComparison.OrdinalIgnoreCase);
        public int CompareTo(IPathBase obj) => CompareTo(obj?.FullPath);
        public int CompareTo(object obj) => obj is string ? CompareTo((string)obj) : CompareTo((obj as IPathBase)?.FullPath);
        public bool Equals(string other) => CompareTo(other) == 0;
        public bool Equals(IPathBase other) => CompareTo(other?.FullPath) == 0;
        protected int UpdatesCounter = 0;
        public bool IsUpdating => UpdatesCounter > 0;
        public virtual void BeginUpdate()
        {
            if (++UpdatesCounter == 1)
                UpdateStarted();
        }

        public virtual void EndUpdate()
        {
            if (UpdatesCounter > 0 && --UpdatesCounter == 0)
                UpdateEnded();
        }

        protected virtual void UpdateStarted()
        {
        }

        protected virtual void UpdateEnded()
        {
            NotifyPropertyChanged();
        }

        public PathBase(string name)
        {
            _Name = name;
        }
    }
}
