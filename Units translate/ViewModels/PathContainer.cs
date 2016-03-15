using System.Collections.Generic;
using Core;

namespace Units_translate
{
    public class PathContainer : BindableBase
    {
        protected string _Path;
        public string FullPath { get { return _Path; } }
        public virtual string Path { get { return _Path; } }
        public virtual string Name { get { return System.IO.Path.GetFileName(_Path); } }
        public string FileName { get { return System.IO.Path.GetFileName(_Path); } }
        public virtual string Ext { get { return string.Empty; } }
        public virtual int StringsCount { get { return 0; } }
        public virtual int CyrilicCount { get { return 0; } }
        public virtual IEnumerable<IMapItem> ShowingItems { get { return null; } }

        public override string ToString()
        {
            return Name;
        }

        public PathContainer(string fullpath)
        {
            _Path = fullpath;
        }
    }
}
