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
        /// <summary>
        /// Индекс начала области редактирования. Используется для определения области замены текста, т.к. Start может захватывать символы не входящие в значение
        /// </summary>
        public override int EditStart => _ValueStart;
        /// <summary>
        /// Индекс конца области редактирования. Используется для определения области замены текста, т.к. End может захватывать символы не входящие в значение
        /// </summary>
        public override int EditEnd => _ValueEnd;

        public MapItemRangedValueBase(string value, int start, int end, int valueStart, int valueEnd): base (value, start, end)
        {
            _ValueStart = valueStart;
            _ValueEnd = valueEnd;
        }
    }
}