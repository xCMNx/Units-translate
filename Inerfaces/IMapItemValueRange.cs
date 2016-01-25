namespace Core
{
    public interface IMapItemValueRange
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