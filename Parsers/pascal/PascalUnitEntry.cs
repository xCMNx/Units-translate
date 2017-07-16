using Core;
using System.Collections.Generic;

namespace pascal
{
    public class PascalUnitEntry : MapUnitEntryBase
    {
        public override bool CaseSensitive => false;
        public override string ToUnitString() => $"{Value} in '{Path}',";
        public PascalUnitEntry(string value, int start, int end, List<IMapUnitLink> links) : base(value, start, end, links)
        {
        }
    }
}
