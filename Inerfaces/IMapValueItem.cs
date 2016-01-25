namespace Core
{
    /// <summary>
    /// Область разметки
    /// </summary>
    public interface IMapValueItem : IMapItemRange
    {
        /// <summary>
        /// Индекс начала области редактирования
        /// </summary>
        int EditStart { get; }

        /// <summary>
        /// Индекс конца области редактирования
        /// </summary>
        int EditEnd { get; }

        /// <summary>
        /// Значение области
        /// </summary>
        string Value { get; }

        /// <summary>
        /// Преобразует переданную строку в вариант которй должен находиться в тексте
        /// </summary>
        /// <param name = "value">Исходное значение</param>
        /// <returns>Исходное значение преобразованное для записи</returns>
        string NewValue(string value);
    }
}