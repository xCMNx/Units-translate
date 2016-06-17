namespace Core
{
    public class MapMethodItemBase : MapValuedItemBase, IMapMethodItem
    {
        public MapMethodItemBase(string value, int start, int end): base (value, start, end)
        {
        }

        public override bool IsSameValue(object val)
        {
            return base.IsSameValue(val) || (val as IMapMethodItem)?.Value == Value;
        }
    }
}
