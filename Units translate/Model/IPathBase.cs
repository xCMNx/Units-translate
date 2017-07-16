using System;

namespace Units_translate
{
    public interface IPathBase : IComparable
    {
        string FullPath { get; }
        string Path { get; }
        string Name { get; }
        string Ext { get; }
        string[] FullPathParts { get; }
    }
}
