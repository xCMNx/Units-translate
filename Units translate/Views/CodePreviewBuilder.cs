using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Core;

namespace Units_translate.Views
{
    static class CodePreviewBuilder
    {
        static Thickness lineNumPadding = new Thickness() { Left = 5, Right = 10 };
        static SolidColorBrush numLineBrush = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0));

        static void AddLine(Grid grid, int number, string value, Brush fore, Brush numFore)
        {
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            var ctrl = new TextBlock() { Text = number.ToString(), Padding = lineNumPadding, Background = numLineBrush, Foreground = numFore };
            grid.Children.Add(ctrl);
            Grid.SetRow(ctrl, grid.RowDefinitions.Count - 1);
            Grid.SetColumn(ctrl, 0);
            ctrl = new TextBlock() { Text = value, Foreground = fore };
            grid.Children.Add(ctrl);
            Grid.SetRow(ctrl, grid.RowDefinitions.Count - 1);
            Grid.SetColumn(ctrl, 1);
        }

        static void AddLine(StackPanel nums, StackPanel vals, int number, string value, Brush fore, Brush numFore)
        {
            nums.Children.Add(new TextBlock() { Text = number.ToString(), Padding = lineNumPadding, Background = numLineBrush, Foreground = numFore });
            vals.Children.Add(new TextBlock() { Text = value, Foreground = fore });
        }

        public class PreviewMap
        {
            public readonly List<int> LineStart;
            public readonly List<int> LineLength;
            public readonly string Code;

            static Regex linesRegex = new Regex(@"(?ixmn)(?<sl>^).*?(?<el>$)");
            public PreviewMap(string code)
            {
                Code = code;
                var matches = linesRegex.Matches(code);
                LineStart = new List<int>(matches.Count);
                LineLength = new List<int>(matches.Count);
                foreach (Match m in matches)
                {
                    var idx = m.Groups["sl"].Index;
                    LineStart.Add(idx);
                    LineLength.Add(m.Groups["el"].Index - idx - 1);
                }
            }

            public int LineIndexAt(int carretPos)
            {
                int line = LineStart.BinarySearch(carretPos);
                if (line < 0)
                    return ~line - 1;
                return line;
            }

            public string LineAt(int line) => LineLength[line] > 0 ? Code.Substring(LineStart[line], LineLength[line]) : string.Empty;
        }

        public static Tuple<UIElement, Tuple<int, int, string>> MakePreview(PreviewMap map, IMapValueItem itm, int previwCount, string value)
        {
            var grid = new Grid() { VerticalAlignment = VerticalAlignment.Stretch };
            Tuple<UIElement, Tuple<int, int, string>> res = new Tuple<UIElement, Tuple<int, int, string>>(grid, null);
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition() /*{ Width = GridLength.Auto }*/);
            var nums = new StackPanel();
            Grid.SetColumn(nums, 0);
            var vals = new StackPanel();
            var sb = new ScrollViewer() { Content = vals, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled };
            Grid.SetColumn(sb, 1);
            grid.Children.Add(nums);
            grid.Children.Add(sb);
            var changed = !string.IsNullOrWhiteSpace(value);

            var lIdx = map.LineIndexAt(itm.Start);
            var lIdx2 = map.LineIndexAt(itm.End);
            var lastLine = Math.Min(lIdx2 + previwCount, map.LineStart.Count - 1);
            for (var line = Math.Max(lIdx - previwCount, 0); line <= lastLine; line++)
            {
                var brush = (line >= lIdx && line <= lIdx2) ? Brushes.Green : Brushes.Black;
                AddLine(nums, vals, line + 1, map.LineAt(line), brush, brush);
                if (changed && line == lIdx2)
                {
                    var rIdx = map.LineStart[lIdx];
                    var rLen = map.LineStart[lIdx2] + map.LineLength[lIdx2] - map.LineStart[lIdx];
                    string[] lines = map.Code.Substring(rIdx, rLen).
                            Remove(itm.EditStart - map.LineStart[lIdx], itm.EditEnd - itm.EditStart).
                            Insert(itm.EditStart - map.LineStart[lIdx], value).Replace("\r\n", "\n").Split('\n');
                    if (lines.Length > 1)
                    {
                        var idx = lIdx;
                        var tab = lines[0];
                        //ищем отступ
                        for (int i = 0; i < tab.Length; i++)
                        {
                            if (!char.IsWhiteSpace(tab[i]))
                            {
                                //оригинальный отступ + 2а пробела
                                tab = tab.Substring(0, i) + "  ";
                                break;
                            }
                        }
                        //добавим отступы к строкам
                        for (int i = 1; i < lines.Length; i++)
                            lines[i] = tab + lines[i];
                        //выведем строки в предпросмотр
                        foreach (var ln in lines)
                            AddLine(nums, vals, ++idx, ln, Brushes.Red, Brushes.Red);
                    }
                    else if (lines.Length == 1)
                        AddLine(nums, vals, lIdx + 1, lines[0], Brushes.Red, Brushes.Red);
                    //данные для замены в файле, индекс начала, длина, и текст на замену
                    res = new Tuple<UIElement, Tuple<int, int, string>>(res.Item1, new Tuple<int, int, string>(rIdx, rLen, string.Join("\r\n", lines)));
                }
            }
            return res;
        }
    }
}
