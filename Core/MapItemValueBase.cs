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

        public int EditStart => _EditStart();

        public int EditEnd => _EditEnd();

        protected virtual int _EditStart() => _Start;
        protected virtual int _EditEnd() => _End;

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
            return string.Format("{0}: {1}", Start, Value);
        }
    }
}