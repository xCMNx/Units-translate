namespace Core
{
    public class MapValueItemBase : MapValuedItemBase, IMapValueItem
    {
        /// <summary>
        /// Индекс начала области редактирования. Используется для определения области замены текста, т.к. Start может захватывать символы не входящие в значение
        /// </summary>
        public virtual int EditStart => _Start;
        /// <summary>
        /// Индекс конца области редактирования. Используется для определения области замены текста, т.к. End может захватывать символы не входящие в значение
        /// </summary>
        public virtual int EditEnd => _End;
        public MapValueItemBase(string value, int start, int end): base (value, start, end)
        {
        }

        /// <summary>
        /// Преобразует переданную строку в вариант которых должен находиться в тексте
        /// </summary>
        /// <param name = "value">Исходное значение</param>
        /// <returns>Исходное значение преобразованное для записи</returns>
        public virtual string NewValue(string value)
        {
            return value;
        }

        public override bool IsSameValue(object val)
        {
            return base.IsSameValue(val) || (val as IMapValueItem)?.Value == Value;
        }
    }
}
