using System.Windows.Media;

namespace Core
{
    public class MapItemBackgroundColorRangeBase : MapItemRangeBase, IMapBackgroundColorRange
    {
        Brush _BackgroundColor;
        public Brush BackgroundColor => _BackgroundColor;
        public MapItemBackgroundColorRangeBase(int start, int end, Brush backgroundColor): base (start, end)
        {
            _BackgroundColor = backgroundColor;
        }
    }
}