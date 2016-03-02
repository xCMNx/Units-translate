using System;

namespace Core
{
    public class MapItemMethodBase : MapItemRangeBase, IMapMethodItem
    {
        IMapRecord _Value;
        /// <summary>
        /// Значение области
        /// </summary>
        public string Value => _Value?.Value;
        public MapItemMethodBase(string value, int start, int end): base (start, end)
        {
            _Value = MappedData.GetMethodRecord(value);
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", Start, Value);
        }

        public bool Equals(IMapMethodItem other)
        {
            throw new NotImplementedException();
        }

        public bool IsSameValue(object val)
        {
            var v = val as MapItemMethodBase;
            if (v != null)
                return _Value == v._Value;

            var vl = val as IMapRecord;
            if (val != null)
                return _Value == val;

            return false;
        }
    }
}