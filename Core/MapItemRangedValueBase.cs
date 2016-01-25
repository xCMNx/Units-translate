namespace Core
{
    public class MapItemRangedValueBase : MapItemValueBase, IMapItemValueRange
    {
        int _ValueStart;
        int _ValueEnd;
        /// <summary>
        /// Индекс начала значения
        /// </summary>
        public int ValueStart => _ValueStart;
        /// <summary>
        /// Индекс конца значения
        /// </summary>
        public int ValueEnd => _ValueEnd;

        protected override int _EditStart() => _ValueStart;
        protected override int _EditEnd() => _ValueEnd;

        public MapItemRangedValueBase(string value, int start, int end, int valueStart, int valueEnd): base (value, start, end)
        {
            _ValueStart = valueStart;
            _ValueEnd = valueEnd;
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", _ValueStart, Value);
        }
    }
}