namespace Core
{
    public interface IMapMethodItem : IMapItemRange
    {
        /// <summary>
        /// Значение области
        /// </summary>
        string Value
        {
            get;
        }

        /// <summary>
        /// Одинаковые ли значения
        /// </summary>
        /// <param name="val">Сравниваемый объект</param>
        /// <returns></returns>
        bool IsSameValue(object val);
    }
}