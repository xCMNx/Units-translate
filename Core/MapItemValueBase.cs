using System;

namespace Core
{
    public class MapItemValueBase : MapItemRangeBase, IMapValueItem
    {
        IMapRecord _Value;
        /// <summary>
        /// Значение области
        /// </summary>
        public string Value => _Value?.Value;
        /// <summary>
        /// Индекс начала области редактирования. Используется для определения области замены текста, т.к. Start может захватывать символы не входящие в значение
        /// </summary>
        public virtual int EditStart => _Start;
        /// <summary>
        /// Индекс конца области редактирования. Используется для определения области замены текста, т.к. End может захватывать символы не входящие в значение
        /// </summary>
        public virtual int EditEnd => _End;
        public MapItemValueBase(string value, int start, int end): base (start, end)
        {
            _Value = MappedData.GetValueRecord(value);
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

        public override string ToString()
        {
            return string.Format("{0}: {1}", EditStart, Value);
        }

        public bool IsSameValue(object val)
        {
            var v = val as MapItemValueBase;
            if (v != null)
                return _Value == v._Value;

            var vl = val as IMapRecord;
            if (val != null)
                return _Value == val;

            return false;
        }
    }
}