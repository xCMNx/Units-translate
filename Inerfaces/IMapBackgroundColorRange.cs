using System.Windows.Media;

namespace Core
{
    public interface IMapBackgroundColorRange : IMapItemRange
    {
        /// <summary>
        /// Цвет заливки
        /// </summary>
        Brush BackgroundColor { get; }
    }
}