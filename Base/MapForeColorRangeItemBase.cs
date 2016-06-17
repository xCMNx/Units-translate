using System.Windows.Media;

namespace Core
{
    public class MapForeColorRangeItemBase : MapRangeItemBase, IMapForeColorRange
    {
        Brush _ForegroundColor;
        public Brush ForegroundColor => _ForegroundColor;
        public MapForeColorRangeItemBase(int start, int end, Brush foregroundColor) : base (start, end)
        {
            _ForegroundColor = foregroundColor;
        }
    }
}
