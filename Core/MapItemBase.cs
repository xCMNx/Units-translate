namespace Core
{
    /// <summary>
    /// Базовый класс области разметки
    /// </summary>
    public class MapItemBase : IMapItem
    {
        int _Start;
        int _End;
        int _ValueStart;
        int _ValueEnd;
        IMapRecord _Value;
        MapItemType _ValueType;

        /// <summary>
        /// Индекс начала области
        /// </summary>
        public int Start => _Start;
        /// <summary>
        /// Индекс конца области
        /// </summary>
        public int End => _End;
        /// <summary>
        /// Индекс начала значения
        /// </summary>
        public int ValueStart => _ValueStart;
        /// <summary>
        /// Индекс конца значения
        /// </summary>
        public int ValueEnd => _ValueEnd;
        /// <summary>
        /// Значение области
        /// </summary>
        public string Value => _Value?.Value;
        /// <summary>
        /// Тип области
        /// </summary>
        public MapItemType ItemType => _ValueType;

        public MapItemBase(string value, int start, int end, int valuestart, int valueend, MapItemType type)
        {
            _Start = start;
            _End = end;
            _ValueStart = valuestart;
            _ValueEnd = valueend;
            _ValueType = type;
            _Value = MappedData.GetValueRecord(value, _ValueType);
        }

        public MapItemBase(string value, int start, int end, MapItemType type)
            : this(value, start, end, start, end, type)
        {
        }

        /// <summary>
        /// Преобразует переданную строку в вариант которых должен находиться в тексте
        /// </summary>
        /// <param name="value">Исходное значение</param>
        /// <returns>Исходное значение преобразованное для записи</returns>
        public virtual string NewValue(string value)
        {
            return value;
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", ValueStart, Value);
        }
    }
}
