using System.Linq;

namespace Units_translate
{
    public class DirectoryContainer : PathPart, IPathNode
    {
        public PathPart Parent { get; protected set; }

        public override string Path => System.IO.Path.Combine(Parent.Path, Name);
        public override string FullPath => Path;
        public override string[] FullPathParts => Parent.FullPathParts.Concat(new string[] { Name }).ToArray();

        public override void Dispose()
        {
            Parent.Remove(this);
            base.Dispose();
        }

        public DirectoryContainer(PathPart parent, string name) : base(name)
        {
            Parent = parent;
            Parent.Add(this);
        }
    }
}
