#define MAPPED_DATA_OPTIMIZE
namespace Core
{
    public class MapValuedItemBase : MapRangeItemBase, IMapBaseItem
#if MAPPED_DATA_OPTIMIZE
        , IMapOptimizableValueItem
    {
        object _value;
        bool _optimized = false;

        public void SwapValueToMapRecord(IMapRecord record)
        {
            _value = record;
            _optimized = true;
        }
        /// <summary>
        /// Значение области
        /// </summary>
        public string Value => _optimized ? (_value as IMapRecord).Value : (string)_value;
        public MapValuedItemBase(string value, int start, int end): base (start, end)
        {
            _value = value;
        }
#else
    {

        /// <summary>
        /// Значение области
        /// </summary>
        public string Value { get; protected set; }
        public MapValuedItemBase(string value, int start, int end) : base(start, end)
        {
            Value = value;
        }
#endif

        public override string ToString()
        {
            return string.Format("{0}: {1}", Start, Value);
        }

        public virtual bool IsSameValue(object val)
        {
            return (val as IMapRecord)?.Value == Value;
        }
    }
}
