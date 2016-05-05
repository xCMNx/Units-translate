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
    [MapperFilter(new[]{".dfm", ".pas", ".dpr"})]
    public class PascalMapper : IMapper
    {
        public string Name => "pascal"; 
        static Brush InterfaceBrush = Brushes.Gray;
        static Brush CommentBrush = Brushes.Green;
        static Brush DirectiveBrush = Brushes.LightBlue;

        static Regex regex = new Regex(@"#(\d+)");

        static Regex regexUses = new Regex(@"(?ixms)
 (?:\{.*?\})#skip comments
|(?:\(\*.*?\*\))#skip comments
|(?://.*?[\0\r\n])#skip comments
|(?:'(?:[^'\0\r\n]*|'')*?')#skip strings
|(?<uses>\buses\b(?:[^;\{/(']*|\{.*?}|//.*?[\0\r\n]|\((?:\*.*?\*\)|.*)|'(?:[^'\0\r\n]*|'')*?')*?;)
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

        static Regex regexTr = new Regex(@"(?ixm)
 (?:\{.*?\})#skip comments
|(?:\(\*.*?\*\))#skip comments
|(?://.*?[\0\r\n])#skip comments
|(?:'(?:[^'\0\r\n]*|'')*?')#skip strings
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

        public enum PascalFileType
        {
            dfm,
            pas,
            dpr
        }

        public ICollection<IMapItemRange> Parse(string Text, PascalFileType pType)
        {
            var res = new List<IMapItemRange>();
                int Start = -1, End = -1, newWordStart = -1;
                bool uses = false;
                var value = string.Empty;
                var comb = 0;
                var word = string.Empty;
                var wordStart = -1;
                Action<int> setWord = pType == PascalFileType.dfm ? (_ => { }) : (Action<int>)((idx) =>
                {
                    if (newWordStart > -1)
                    {
                        wordStart = newWordStart;
                        word = Text.Substring(newWordStart, idx - newWordStart);
                        uses = uses || word.Equals("uses", StringComparison.InvariantCultureIgnoreCase);
                        newWordStart = -1;
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
                            res.Add(new MapItemForeColorRangeBase(start, idx + 1, CommentBrush));
                        }
                        break;

                    case '{':
                        {
                            var start = idx;
                            while (++idx < Text.Length && Text[idx] != '}') ;
                            res.Add(new MapItemForeColorRangeBase(start, idx + 1, (idx > start && Text[start + 1] == '$') ? DirectiveBrush : CommentBrush));
                        }
                        break;

                    case '*':
                        if (idx > 0 && Text[idx - 1] == '(')
                        {
                            var start = idx - 1;
                            while (++idx < Text.Length && !(Text[idx] == ')' && Text[idx - 1] == '*'))
                                ;
                            res.Add(new MapItemForeColorRangeBase(start, idx + 1, CommentBrush));
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
                                    res.Add(new MapItemForeColorRangeBase(Start, End, Brushes.Red));
                                else if (regexGUID.IsMatch(value))
                                    res.Add(new MapItemForeColorRangeBase(Start, End, InterfaceBrush));
                                else
                                    res.Add(new PascalMapItem(value, Start, End));
                                Start = -1;
                                comb = 0;
                                value = string.Empty;
                            }

                            if (uses)
                                uses = Text[idx] != ';';

                            if (Start == -1)
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
                                        if (pType != PascalFileType.dfm)
                                        {
                                            uses = false;
                                            setWord(idx);
                                            methods.Push(new KeyValuePair<string, int>(word, wordStart));
                                            wordStart = -1;
                                        }
                                        continue;
                                    case ')':
                                        newWordStart = -1;
                                        if (pType != PascalFileType.dfm && methods.Count > 0)
                                        {
                                            var method = methods.Pop();
                                            if (method.Value > -1
                                                && !method.Key.Equals("class", StringComparison.InvariantCultureIgnoreCase)
                                                && !method.Key.Equals("if", StringComparison.InvariantCultureIgnoreCase)
                                                && !method.Key.Equals("or", StringComparison.InvariantCultureIgnoreCase)
                                                && !method.Key.Equals("and", StringComparison.InvariantCultureIgnoreCase)
                                                && !method.Key.Equals("xor", StringComparison.InvariantCultureIgnoreCase)
                                              )
                                                res.Add(new MapItemMethodBase(method.Key, method.Value, idx + 1));
                                        }
                                        break;
                                    case ' ':
                                    case '\t':
                                    case '\r':
                                    case '\n':
                                        setWord(idx);
                                        continue;
                                    default:
                                        comb = 2;
                                        if (char.IsLetterOrDigit(Text[idx]) || Text[idx] == '_')
                                        {
                                            if (newWordStart == -1)
                                                newWordStart = idx;
                                        }
                                        else
                                        {
                                            word = string.Empty;
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

        public ICollection<IMapItemRange> GetMap(string Text, string Ext)
        {
            PascalFileType pType = PascalFileType.pas;
            if (".dpr".Equals(Ext, StringComparison.InvariantCultureIgnoreCase))
                pType = PascalFileType.dpr;
            else if (".dfm".Equals(Ext, StringComparison.InvariantCultureIgnoreCase))
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
            var p = System.Diagnostics.Process.Start(new ProcessStartInfo(converterPath, string.Format("-i -t \"{0}\"", FilePath))
            {CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true, });
            p.WaitForExit();
            string output = p.StandardOutput.ReadToEnd();
            if (output.Contains("Error converting"))
                throw new Exception(output);
        }
    }
}