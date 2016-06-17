namespace Core
{
    public class MapRangeItemBase : IMapRangeItem
    {
        protected int _Start;
        protected int _End;
        /// <summary>
        /// Индекс начала области
        /// </summary>
        public int Start => _Start;
        /// <summary>
        /// Индекс конца области
        /// </summary>
        public int End => _End;
        public MapRangeItemBase(int start, int end)
        {
            _Start = start;
            _End = end;
        }
    }
}
