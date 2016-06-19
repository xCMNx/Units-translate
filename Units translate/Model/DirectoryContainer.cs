using System.Linq;

namespace Units_translate
{
    public class DirectoryContainer : PathPart, IPathNode
    {
        public PathPart Parent { get; protected set; }

        public override string Path => Parent.FullPath;
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
