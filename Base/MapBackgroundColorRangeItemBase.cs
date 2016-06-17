using System.Windows.Media;

namespace Core
{
    public class MapBackgroundColorRangeItemBase : MapRangeItemBase, IMapBackgroundColorRange
    {
        Brush _BackgroundColor;
        public Brush BackgroundColor => _BackgroundColor;
        public MapBackgroundColorRangeItemBase(int start, int end, Brush backgroundColor): base (start, end)
        {
            _BackgroundColor = backgroundColor;
        }
    }
}
