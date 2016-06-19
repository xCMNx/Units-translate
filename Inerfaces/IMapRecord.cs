using System;

namespace Core
{
    public interface IMapRecord : IComparable, IEquatable<string>, IEquatable<IMapRecord>
    {
        string Value
        {
            get;
        }
    }
}