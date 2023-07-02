using Core;

namespace pascal
{
    internal class PascalResourceMapItem : MapUnitPathBase, IMapSubrange
    {
        IMapRangeItem _subrange;
        public IMapRangeItem Subrange => _subrange;

        public PascalResourceMapItem(int start, int end, IMapRangeItem subrange) : base(start, end)
        {
            _subrange = subrange;
        }
    }
}
