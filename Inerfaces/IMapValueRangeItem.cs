namespace Core
{
    public interface IMapValueRangeItem
    {
        /// <summary>
        /// Индекс начала области значения
        /// </summary>
        int ValueStart { get; }

        /// <summary>
        /// Индекс конца области значения
        /// </summary>
        int ValueEnd { get; }
    }
}
