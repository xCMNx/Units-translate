using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Core;

namespace pascal
{
    public class PascalMapItem : MapItemBase
    {
        /// <summary>
        /// Преобразует строку в вариант для паскаля
        /// </summary>
        /// <param name="value">Исправляемое значение</param>
        /// <param name="controlOnly">Заменять на хэши только контрольные символы. Если true, то кирилица будет записана, как есть</param>
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

        public PascalMapItem(string value, int start, int end, MapItemType type)
            : base(value, start, end, start, end, type)
        {
        }
    }

    public class DfmMapItem : PascalMapItem
    {
        public override string NewValue(string value)
        {
            return GenNewValue(value, false);
        }

        public DfmMapItem(string value, int start, int end, MapItemType type)
            : base(value, start, end, type)
        {
        }
    }

    [MapperFilter(new[] { ".dfm", ".pas", ".dpr"})]
    public class PascalMapper : IMapper
    {
        public string Name { get { return "pascal"; } }

        static Regex regex = new Regex(@"#(\d+)");
        static Regex regexUses = new Regex(@"(?ixmsn)
 (?:\{.*?\})#skip comments
|(?:\(\*.*?\*\))#skip comments
|(?://.*?$)#skip comments
|(?:'(?:[^']+|'')+')#skip strings
|(?<uses>\buses(?:[^;\{/(']*|\{.*?}|//.*?$|\(\*.*?\*\)|'([^']+|'')*')+;)");
        static Regex regexGUID = new Regex(@"\{[a-fA-F\d]{8}-[a-fA-F\d]{4}-[a-fA-F\d]{4}-[a-fA-F\d]{4}-[a-fA-F\d]{12}\}");

        public enum PascalFileType { dfm, pas, dpr}

        public IEnumerable<IMapItem> Parse(string Text, PascalFileType pType)
        {
            var res = new List<IMapItem>();
            int Start = -1,
                End = -1,
                uStart = -1,
                uEnd = -1;
            //строки внутри блока Uses это пути к файлам, их точно индексировать не нужно
            //поэтому запомним область блока
            if (pType == PascalFileType.dpr)
            {
                foreach (Match m in regexUses.Matches(Text))
                    if (m.Groups["uses"].Success)
                    {
                        uStart = m.Index;
                        uEnd = m.Index + m.Length;
                        break;
                    }
            }
            var value = string.Empty;
            var comb = 0;
            for (int i = 0; i < Text.Length; i++)
            {
                switch (Text[i])
                {
                    case '\0':
                        if(pType == PascalFileType.dfm)
                            throw new MapperFixableException("Символ конца строки внутри строки. Возможно бинарный формат *.dfm.");
                        else
                            throw new MapperException("Символ конца строки внутри строки.");
                    case '/':
                        if (i > 0 && Text[i - 1] == '/')
                        {
                            var start = i - 1;
                            while (++i < Text.Length && !(Text[i] == '\r' || Text[i] == '\n')) ;
                            res.Add(new MapItemBase(Text.Substring(start + 1, i - start - 1), start, i + 1, MapItemType.Commentary));
                        }
                        break;
                    case '{':
                        {
                            var start = i;
                            while (++i < Text.Length && Text[i] != '}') ;
                            res.Add(new MapItemBase(Text.Substring(start + 1, i - start - 1), start, i + 1, (i > start && Text[start + 1] == '$') ? MapItemType.Directive : MapItemType.Commentary));
                        }
                        break;
                    case '*':
                        if (i > 0 && Text[i - 1] == '(')
                        {
                            var start = i - 1;
                            while (++i < Text.Length && !(Text[i] == ')' && Text[i - 1] == '*')) ;
                            res.Add(new MapItemBase(Text.Substring(start, i - start + 1), start, i + 1, MapItemType.Commentary));
                        }
                        break;
                    case '#':
                        {
                            if (Start == -1)
                                Start = i;
                            int start = i;
                            while (++i < Text.Length && (char.IsNumber(Text[i]) || Text[i] == '#')) ;
                            string str = Text.Substring(start, i - start);
                            MatchCollection ms = regex.Matches(str);
                            foreach (Match m in ms)
                                value += Convert.ToChar(Convert.ToInt32(m.Groups[1].Value));
                            End = i--;
                        }
                        break;
                    case '\'':
                        {
                            if (Start == -1)
                                Start = i;
                            int start = i;
                            int cnt;
                            do
                            {
                                while (++i < Text.Length && Text[i] != '\'') ;
                                cnt = 0;
                                while (i < Text.Length && Text[i] == '\'')
                                {
                                    cnt++;
                                    i++;
                                }
                            } while (cnt != 0 && cnt % 2 == 0);
                            value += Text.Substring(start + 1, i - start - 2);
                            End = i--;
                        }
                        break;
                    default:
                        if (Start != -1)
                        {
                            if (comb == 1 && res.Count > 0)
                            {
                                //если был найден только + между строками и предыдущее значение позволяет выполнить сложение строк
                                var prev = res[res.Count - 1];
                                if (prev.ItemType == MapItemType.String)
                                {
                                    res.RemoveAt(res.Count - 1);
                                    value = prev.Value + value;
                                    Start = prev.Start;
                                }
                            }
                            if (pType == PascalFileType.dfm)
                                res.Add(new DfmMapItem(value, Start, End, regexGUID.IsMatch(value) ? MapItemType.Interface : MapItemType.String));
                            else if (Start > uStart && Start < uEnd)
                                //если строка попала в область Uses то запишем её как пустой тип значения, тогда разметка в файле будет но не строковая
                                res.Add(new PascalMapItem(value, Start, End, regexGUID.IsMatch(value) ? MapItemType.Interface : MapItemType.None));
                            else
                                res.Add(new PascalMapItem(value, Start, End, regexGUID.IsMatch(value) ? MapItemType.Interface : MapItemType.String));
                            Start = -1;
                            comb = 0;
                            value = string.Empty;
                        }
                        //лабуда для записей вида s := 'Error raised at :' + date + #13#10 + 'In file:'#13#10 + filepath
                        //позволяет объединить следующую строку с предыдущей если между ними был только +
                        // в итоге получим 'Error raised at :' и #13#10'In file:'#13#10
                        // вместо 'Error raised at :', #13#10, 'In file:'#13#10
                        switch (Text[i])
                        {
                            case '+': comb++; break;
                            case ' ': case '\t': case '\r': case '\n': break;
                            default: comb = 2; break;
                        }
                        break;
                }
            }
            return res;
        }

        public IEnumerable<IMapItem> GetMap(string Text, string Ext)
        {
            PascalFileType pType = PascalFileType.pas;
            if (".dpr".Equals(Ext, StringComparison.InvariantCultureIgnoreCase))
                pType = PascalFileType.dpr;
            else if(".dfm".Equals(Ext, StringComparison.InvariantCultureIgnoreCase))
                pType = PascalFileType.dfm;
            return Parse(Text, pType);
        }

        public bool IsExtAcceptable(string Ext)
        {
            return true;
        }

        static string asmPath = Helpers.AssemblyDirectory(System.Reflection.Assembly.GetExecutingAssembly());
        static string converterPath = Path.Combine(asmPath, "convert.exe");

        public void TryFix(string FilePath, Encoding encoding)
        {
            var p = System.Diagnostics.Process.Start(new ProcessStartInfo(converterPath, string.Format("-i -t \"{0}\"", FilePath)) {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
            });
            p.WaitForExit();
            string output = p.StandardOutput.ReadToEnd();
            if (output.Contains("Error converting"))
                throw new Exception(output);
        }
    }
}
