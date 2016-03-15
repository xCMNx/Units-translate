namespace Core
{
    public enum MapItemType { String, Interface, Commentary, Directive, None };

    /// <summary>
    /// Область разметки
    /// </summary>
    public interface IMapItem
    {
        /// <summary>
        /// Индекс начала области
        /// </summary>
        int Start { get; }
        /// <summary>
        /// Индекс конца области
        /// </summary>
        int End { get; }
        /// <summary>
        /// Индекс начала значения
        /// </summary>
        int ValueStart { get; }
        /// <summary>
        /// Индекс конца значения
        /// </summary>
        int ValueEnd { get; }
        /// <summary>
        /// Тип области
        /// </summary>
        MapItemType ItemType { get; }
        /// <summary>
        /// Значение области
        /// </summary>
        string Value { get; }
        /// <summary>
        /// Преобразует переданную строку в вариант которй должен находиться в тексте
        /// </summary>
        /// <param name="value">Исходное значение</param>
        /// <returns>Исходное значение преобразованное для записи</returns>
        string NewValue(string value);
    }
}
