using System.Text;
using Core;

namespace pascal
{
    public class PascalMapItem : MapItemRangedValueBase
    {
        /// <summary>
        /// Преобразует строку в вариант для паскаля
        /// </summary>
        /// <param name = "value">Исправляемое значение</param>
        /// <param name = "controlOnly">Заменять на хэши только контрольные символы. Если true, то кирилица будет записана, как есть</param>
        /// <returns></returns>
        protected string GenNewValue(string value, bool controlOnly, byte maxLength = 0)
        {
            int lineLength = 0;
            var str = new StringBuilder();
            var numeric = false;
            foreach (var chr in value)
            {
                lineLength++;
                if (!controlOnly && ((int)chr > 255) || char.IsControl(chr))
                {
                    if (!numeric)
                    {
                        numeric = true;
                        if (str.Length > 0)
                            str.Append('\'');
                    }

                    str.Append(string.Format("#{0}", (int)chr));
                }
                else
                {
                    if (numeric)
                    {
                        str.Append('\'');
                        numeric = false;
                    }

                    if (maxLength > 0 && lineLength >= maxLength && char.IsWhiteSpace(chr))
                    {
                        str.Append("\' +\r\n\'");
                        lineLength = 0;
                    }

                    str.Append(chr);
                    if (chr == '\'')
                        str.Append(chr);
                }
            }

            if (!numeric)
                str.Append('\'');
            var s = str.ToString();
            if (s[0] != '#')
                s = '\'' + s;
            return s;
        }

        public override string NewValue(string value)
        {
            return GenNewValue(value, true, 75);
        }

        public PascalMapItem(string value, int start, int end): base (value, start, end, start, end)
        {
        }
    }
}