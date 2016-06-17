using System.Windows.Media;

namespace Core
{
    public interface IMapForeColorRange : IMapRangeItem
    {
        /// <summary>
        /// Цвет
        /// </summary>
        Brush ForegroundColor { get; }
    }
}