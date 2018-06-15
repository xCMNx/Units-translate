using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media;
using Core;

namespace pascal
{
    [MapperFilter(new[] { ".dfm", ".pas", ".dpr" })]
    public class PascalMapper : IMapper
    {
        public string Name => "pascal";
        static Brush InterfaceBrush = Brushes.Gray;
        static Brush CommentBrush = Brushes.Green;
        static Brush DirectiveBrush = Brushes.LightBlue;

        static Regex regex = new Regex(@"#(\d+)");

        public static Regex regexUses = new Regex(@"(?ixms)
 \{.*?\}#skip comments
|\(\*.*?\*\)#skip comments
|//.*?$#skip comments
|'(?:[^'\0\r\n]*|'')*?'#skip strings
|(?<uses>\b(uses|contains)\b(?:[^;\{/(']*|\{.*?}|//.*?[\0\r\n]|\((?:\*.*?\*\)|.*)|'(?:[^'\0\r\n]*|'')*?')*?;)
        ");

        static Regex regexUsesTr = new Regex(@"(?ixms)
 (?:\{.*?\})#skip comments
|(?:\(\*.*?\*\))#skip comments
|(?://.*?[\0\r\n])#skip comments
|(?:'(?:[^'\0\r\n]*|'')*?')#skip strings
|(?<uses>\buses\b(?:[^;\{/(']*|\{.*?}|//.*?[\0\r\n]|\((?:\*.*?\*\)|.*)|'(?:[^'\0\r\n]*|'')*?')*?;)
|(?<tr>\btr[\s]*?\(
      (?:
          (?<n>\()                   #cycle stack +
          |(?<inner-n>\))            #cycle stack -
          |[^{/()']*
          |\{.*?}
          |//.*?[\0\r\n]
          |\(\*.*?\*\)
          |'(?:[^'\0\r\n]*|'')*?'
      )
*\)(?!(n))                          #while stack is not empty
)
        ");

        static Regex regexGUID = new Regex(@"\{[a-fA-F\d]{8}-[a-fA-F\d]{4}-[a-fA-F\d]{4}-[a-fA-F\d]{4}-[a-fA-F\d]{12}\}");

        public enum PascalFileType
        {
            dfm,
            pas,
            dpr
        }

        public ICollection<IMapRangeItem> Parse(string Text, PascalFileType pType, MapperOptions Options)
        {
            var mapMethods = Options.HasFlag(MapperOptions.MapMethods);
            var res = new List<IMapRangeItem>();
            int Start = -1, End = -1, newWordStart = -1;
            bool uses = false;
            bool unit = false;
            var value = string.Empty;
            var comb = 0;
            var word = string.Empty;
            var wordStart = -1;
            var links = new List<IMapUnitLink>();
            Action<int> setWord2 = (idx) =>
            {
                if (newWordStart > -1)
                {
                    wordStart = newWordStart;
                    word = Text.Substring(newWordStart, idx - newWordStart);
                    newWordStart = -1;
                    if (word.Equals("uses", StringComparison.InvariantCultureIgnoreCase))
                    {
                        word = string.Empty;
                        wordStart = -1;
                        uses = true;
                    }
                }
            };
            Action<int> setWord = pType == PascalFileType.dfm ? (_ => { }) : (Action<int>)((idx) =>
            {
                if (newWordStart > -1)
                {
                    setWord2(idx);
                    if(uses)
                        setWord = setWord2;
                    if (unit)
                    {
                        res.Add(new PascalUnitEntry(word, idx - word.Length, idx, links));
                        setWord = setWord2;
                        wordStart = -1;
                        unit = false;
                    }
                    else if (word.Equals("unit", StringComparison.InvariantCultureIgnoreCase))
                        unit = true;
                }
            });
            Stack<KeyValuePair<string, int>> methods = new Stack<KeyValuePair<string, int>>();
            for (int idx = 0; idx < Text.Length; idx++)
            {
                switch (Text[idx])
                {
                    case '\0':
                        if (pType == PascalFileType.dfm)
                            throw new MapperFixableException("Символ конца строки внутри строки. Возможно бинарный формат *.dfm.");
                        else
                            throw new MapperException("Символ конца строки внутри строки.");

                    case '/':
                        if (idx > 0 && Text[idx - 1] == '/')
                        {
                            var start = idx - 1;
                            while (++idx < Text.Length && !(Text[idx] == '\r' || Text[idx] == '\n')) ;
                            res.Add(new MapForeColorRangeItemBase(start, idx + 1, CommentBrush));
                        }
                        break;

                    case '{':
                        {
                            var start = idx;
                            while (++idx < Text.Length && Text[idx] != '}') ;
                            res.Add(new MapForeColorRangeItemBase(start, idx + 1, (idx > start && Text[start + 1] == '$') ? DirectiveBrush : CommentBrush));
                        }
                        break;

                    case '*':
                        if (idx > 0 && Text[idx - 1] == '(')
                        {
                            var start = idx - 1;
                            while (++idx < Text.Length && !(Text[idx] == ')' && Text[idx - 1] == '*'))
                                ;
                            res.Add(new MapForeColorRangeItemBase(start, idx + 1, CommentBrush));
                        }
                        break;

                    case '#':
                        {
                            if (Start == -1)
                                Start = idx;
                            int start = idx;
                            while (++idx < Text.Length && (char.IsNumber(Text[idx]) || Text[idx] == '#'))
                                ;
                            string str = Text.Substring(start, idx - start);
                            MatchCollection ms = regex.Matches(str);
                            foreach (Match m in ms)
                                value += Convert.ToChar(Convert.ToInt32(m.Groups[1].Value));
                            End = idx--;
                        }
                        break;

                    case '\'':
                        {
                            if (Start == -1)
                                Start = idx;
                            int start = idx;
                            int cnt;
                            do
                            {
                                while (++idx < Text.Length && Text[idx] != '\'')
                                    ;
                                cnt = 0;
                                while (idx < Text.Length && Text[idx] == '\'')
                                {
                                    cnt++;
                                    idx++;
                                }
                            }
                            while (cnt != 0 && cnt % 2 == 0);
                            value += Text.Substring(start + 1, idx - start - 2);
                            End = idx--;
                        }
                        break;

                    default:
                        {
                            if (Start != -1)
                            {
                                if (comb == 1 && res.Count > 0)
                                {
                                    //если был найден только + между строками и предыдущее значение позволяет выполнить сложение строк
                                    var prev = res[res.Count - 1] as IMapValueItem;
                                    if (prev != null)
                                    {
                                        res.RemoveAt(res.Count - 1);
                                        value = prev.Value + value;
                                        Start = prev.Start;
                                    }
                                }

                                if (pType == PascalFileType.dfm)
                                    res.Add(new DfmMapItem(value, Start, End));
                                else if (uses)
                                    //если строка попала в область Uses то запишем её как пустой тип значения, тогда разметка в файле будет но не строковая
                                    res.Add(new MapForeColorRangeItemBase(Start, End, Brushes.Red));
                                else if (regexGUID.IsMatch(value))
                                    res.Add(new MapForeColorRangeItemBase(Start, End, InterfaceBrush));
                                else
                                    res.Add(new PascalMapItem(value, Start, End));
                                Start = -1;
                                comb = 0;
                                value = string.Empty;
                            }
                            //else
                            {
                                //лабуда для записей вида s := 'Error raised at :' + date + #13#10 + 'In file:'#13#10 + filepath
                                //позволяет объединить следующую строку с предыдущей если между ними был только +
                                // в итоге получим 'Error raised at :' и #13#10'In file:'#13#10
                                // вместо 'Error raised at :', #13#10, 'In file:'#13#10
                                switch (Text[idx])
                                {
                                    case '+':
                                        comb++;
                                        newWordStart = -1;
                                        continue;
                                    case '(':
                                        //после метода никаких объединений строк быть не может
                                        comb = 2;
                                        if (mapMethods && pType != PascalFileType.dfm)
                                        {
                                            uses = false;
                                            setWord(idx);
                                            methods.Push(new KeyValuePair<string, int>(word, wordStart));
                                            wordStart = -1;
                                        }
                                        continue;
                                    case ')':
                                        //после метода никаких объединений строк быть не может
                                        comb = 2;
                                        newWordStart = -1;
                                        if (mapMethods && pType != PascalFileType.dfm && methods.Count > 0)
                                        {
                                            var method = methods.Pop();
                                            if (method.Value > -1
                                                && !method.Key.Equals("class", StringComparison.InvariantCultureIgnoreCase)
                                                && !method.Key.Equals("if", StringComparison.InvariantCultureIgnoreCase)
                                                && !method.Key.Equals("or", StringComparison.InvariantCultureIgnoreCase)
                                                && !method.Key.Equals("and", StringComparison.InvariantCultureIgnoreCase)
                                                && !method.Key.Equals("xor", StringComparison.InvariantCultureIgnoreCase)
                                                && !method.Key.Equals("not", StringComparison.InvariantCultureIgnoreCase)
                                              )
                                                res.Add(new MapMethodItemBase(method.Key, method.Value, idx + 1));
                                        }
                                        break;
                                    case ' ':
                                    case '\t':
                                    case '\r':
                                    case '\n':
                                    case ';':
                                    case ',':
                                        setWord(idx);
                                        if (uses)
                                        {
                                            uses = Text[idx] != ';';
                                            if (wordStart > -1)
                                            {
                                                if (!word.Equals("in", StringComparison.InvariantCultureIgnoreCase))
                                                {
                                                    var lnk = new MapUnitLinkBase(word, wordStart, wordStart + word.Length);
                                                    res.Add(lnk);
                                                    links.Add(lnk);
                                                }
                                                wordStart = -1;
                                            }
                                        }
                                        continue;
                                    default:
                                        //любой отличный от вайтспэйса символ отменяет объединение строк
                                        comb = 2;
                                        if (char.IsLetterOrDigit(Text[idx]) || Text[idx] == '_' || ((uses || unit) && Text[idx] == '.'))
                                        {
                                            if (newWordStart == -1)
                                                newWordStart = idx;
                                        }
                                        else
                                        {
                                            wordStart = -1;
                                            newWordStart = -1;
                                        }
                                        break;
                                }
                            }
                        }
                        break;
                }
            }
            return res;
        }

        public ICollection<IMapRangeItem> GetMap(string Text, string Ext, MapperOptions Options)
        {
            PascalFileType pType = PascalFileType.pas;
            if (".dpr".Equals(Ext, StringComparison.InvariantCultureIgnoreCase))
                pType = PascalFileType.dpr;
            else if (".dfm".Equals(Ext, StringComparison.InvariantCultureIgnoreCase))
                pType = PascalFileType.dfm;
            return Parse(Text, pType, Options);
        }

        public bool IsExtAcceptable(string Ext)
        {
            return true;
        }

        static string asmPath = Helpers.AssemblyDirectory(System.Reflection.Assembly.GetExecutingAssembly());
        static string converterPath = Path.Combine(asmPath, "convert.exe");
        public void TryFix(string FilePath, Encoding encoding)
        {
            var p = System.Diagnostics.Process.Start(new ProcessStartInfo(converterPath, string.Format("-i -t \"{0}\"", FilePath))
            {CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true, });
            p.WaitForExit();
            string output = p.StandardOutput.ReadToEnd();
            if (output.Contains("Error converting"))
                throw new Exception(output);
        }
    }
}
