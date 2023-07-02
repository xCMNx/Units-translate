namespace Core
{
    public class MapUnitPathBase: MapRangeItemBase, IMapUnitPath
    {
        public MapUnitPathBase(int start, int end) : base(start, end)
        {
        }

        public virtual string getValidPathString(string path) => path;
        public virtual string getValueFromCode(string code) => code.Substring(Start, End - Start);
        public virtual string replaceValueInCode(string code, string value)
        {
            var newValue = getValidPathString(value);
            var res = code.Remove(Start, End - Start).Insert(Start, newValue);
            _End = Start + newValue.Length;
            return res;
        }
    }
}
