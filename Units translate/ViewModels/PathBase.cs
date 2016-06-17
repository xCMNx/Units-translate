using System;
using System.Collections.Generic;
using Core;

namespace Units_translate
{
    public interface IPathBase
    {
        string FullPath { get; }
        string Path { get; }
        string Name { get; }
    }

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

    public class PathBase : BindableBase, IDisposable, IPathBase, IComparable<IPathBase>, IEquatable<IPathBase>
    {
        protected string _Name;
        public virtual string Path => _Name;
        public virtual string FullPath => _Name;
        public virtual string Name => _Name;
        public virtual int StringsCount => 0;
        public virtual int CyrilicCount => 0;
        public virtual string[] FullPathParts => new string[] { Name };

        public override string ToString() => FullPath;

        public int CompareTo(IPathBase other) => string.Compare(FullPath, other.FullPath, StringComparison.OrdinalIgnoreCase);

        public bool Equals(IPathBase other) => FullPath.Equals(other.FullPath, StringComparison.OrdinalIgnoreCase);

        public virtual void Dispose()
        {
        }

        public PathBase(string name)
        {
            _Name = name;
        }
    }
}
