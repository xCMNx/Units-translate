using Core;

namespace pascal
{
    internal class PascalMapUnitPath : MapUnitPathBase
    {
        public PascalMapUnitPath(int start, int end) : base(start, end)
        {
        }
        public override string getValidPathString(string path) => PascalMapItem.GenNewValue(path, false, 0);
        public override string getValueFromCode(string code)
        {
            var add = code[Start] == '\'' ? 1 : 0;
            return code.Substring(Start + add, End - Start - add * 2);
        }
    }
}
