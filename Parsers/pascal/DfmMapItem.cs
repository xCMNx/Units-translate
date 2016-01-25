using Core;

namespace pascal
{
    public class DfmMapItem : PascalMapItem
    {
        public override string NewValue(string value)
        {
            return GenNewValue(value, false);
        }

        public DfmMapItem(string value, int start, int end): base (value, start, end)
        {
        }
    }
}