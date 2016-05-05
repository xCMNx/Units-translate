using System.Collections.Generic;
using Core;

namespace Units_translate
{
    public class PathContainer : BindableBase
    {
        protected string _Path;
        public string FullPath => _Path;
        public virtual string Path => _Path;
        public virtual string Name => _Path.Length < 4 ? _Path.Substring(0, _Path.Length -1) : System.IO.Path.GetFileName(_Path);
        public string FileName => Name;
        public virtual string Ext => string.Empty;
        public virtual int StringsCount => 0;
        public virtual int CyrilicCount => 0;
        public virtual ICollection<IMapItemRange> ShowingItems => null;

        public override string ToString() => Name;

        public PathContainer(string fullpath)
        {
            _Path = fullpath;
        }
    }
}
