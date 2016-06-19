using System;

namespace Units_translate
{
    public interface IPathBase : IComparable
    {
        string FullPath { get; }
        string Path { get; }
        string Name { get; }
        string[] FullPathParts { get; }
    }
}
