using System;

namespace Core
{
    public interface IMapRecord : IComparable
    {
        string Value
        {
            get;
        }
    }
}