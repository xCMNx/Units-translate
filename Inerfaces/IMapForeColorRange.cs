using System.Windows.Media;

namespace Core
{
    public interface IMapForeColorRange : IMapItemRange
    {
        /// <summary>
        /// Цвет
        /// </summary>
        Brush ForegroundColor { get; }
    }
}