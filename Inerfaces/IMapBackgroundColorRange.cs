using System.Windows.Media;

namespace Core
{
    public interface IMapBackgroundColorRange : IMapRangeItem
    {
        /// <summary>
        /// Цвет заливки
        /// </summary>
        Brush BackgroundColor { get; }
    }
}
