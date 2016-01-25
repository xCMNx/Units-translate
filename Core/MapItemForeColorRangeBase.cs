using System.Windows.Media;

namespace Core
{
    public class MapItemForeColorRangeBase : MapItemRangeBase, IMapForeColorRange
    {
        Brush _ForegroundColor;
        public Brush ForegroundColor => _ForegroundColor;
        public MapItemForeColorRangeBase(int start, int end, Brush foregroundColor) : base (start, end)
        {
            _ForegroundColor = foregroundColor;
        }
    }
}