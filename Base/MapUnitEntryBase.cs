using System;
using System.Collections.Generic;

namespace Core
{
    public class MapUnitEntryBase : MapValuedItemBase, IMapUnitEntry
    {
        string _Path = string.Empty;
        public string Path { get => _Path; set { _Path = value; } }

        public ICollection<IMapUnitLink> Links { get; protected set; }

        public IMapData Data { get; set; }

        public virtual bool CaseSensitive => throw new NotImplementedException();

        public MapUnitEntryBase(string value, int start, int end, List<IMapUnitLink> links): base(value, start, end)
        {
            Links = links;
        }

        public virtual string ToUnitString() => $"{Value}:{Path}";
    }
}
