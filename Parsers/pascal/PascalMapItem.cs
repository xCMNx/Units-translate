using System.Collections.Generic;
using System.Text;
using Core;

namespace pascal
{
    public class PascalMapItem : MapItemRangedValueBase
    {
        bool isCodeChar(char chr, bool controlOnly) => !controlOnly && ((int)chr > 255) || char.IsControl(chr);

        string StringToCodes(string str)
        {
            var strb = new StringBuilder();
            foreach (var chr in str)
                strb.Append($"#{(int)chr}");
            return strb.ToString();
        }

        KeyValuePair<string, bool> GetPair(string str, bool isCode) => new KeyValuePair<string, bool>(isCode ? StringToCodes(str) : str, isCode);

        List<KeyValuePair<string, bool>> StringToParts(string str, bool controlOnly)
        {
            var res = new List<KeyValuePair<string, bool>>();
            if (str.Length > 0)
            {
                var start = 0;                
                bool isCode = isCodeChar(str[0], controlOnly);
                for (var idx = 0; idx < str.Length; idx++)
                {
                    var b = isCodeChar(str[idx], controlOnly);
                    if (b != isCode || (!isCode && char.IsWhiteSpace(str[idx])))
                    {
                        if (b == isCode)
                            idx++;
                        res.Add(GetPair(str.Substring(start, idx - start), isCode));
                        start = idx;
                        if (str.Length <= idx)
                            return res;
                        isCode = isCodeChar(str[idx], controlOnly);
                    }
                }
                if(start < str.Length)
                    res.Add(GetPair(str.Substring(start), isCode));
            }
            return res;
        }

        /// <summary>
        /// Преобразует строку в вариант для паскаля
        /// </summary>
        /// <param name = "value">Исправляемое значение</param>
        /// <param name = "controlOnly">Заменять на хэши только контрольные символы. Если true, то кирилица будет записана, как есть</param>
        /// <returns></returns>
        protected string GenNewValue(string value, bool controlOnly, byte maxLength = 0)
        {
            var pairs = StringToParts(value, controlOnly);
            if (pairs.Count == 0)
                return "''";
            var str = new StringBuilder();
            var pair = pairs[0];
            var isCode = pair.Value;
            int lineLength = pair.Key.Length;
            if (!isCode)
                str.Append('\'');
            str.Append(pair.Key);
            for (int idx = 1; idx < pairs.Count; idx++)
            {
                pair = pairs[idx];
                if (maxLength != 0 && maxLength < lineLength && idx != pairs.Count - 1)
                {
                    if(isCode)
                        str.Append(" +\r\n");
                    else
                        str.Append("' +\r\n'");
                    lineLength = 0;
                }
                else if (pair.Value != isCode)
                    str.Append('\'');
                lineLength += pair.Key.Length;
                str.Append(pair.Key);
                isCode = pair.Value;
            }
            if(!isCode)
                str.Append('\'');
            return str.ToString();
        }

        public override string NewValue(string value)
        {
            return GenNewValue(value, true, 125);
        }

        public PascalMapItem(string value, int start, int end): base (value, start, end, start, end)
        {
        }
    }
}