using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Core;

namespace Units_translate.Views
{
    /// <summary>
    /// Логика взаимодействия для SampleView.xaml
    /// </summary>
    public partial class SampleView
    {
        public SampleView()
        {
            InitializeComponent();
            addBtn = (Style)FindResource("addBtn");
            showBtn = (Style)FindResource("showBtn");
        }

        public static readonly DependencyProperty TranslationProperty = DependencyProperty.Register(
          nameof(Translation),
          typeof(string),
          typeof(SampleView)
        );

        public string Translation
        {
            get { return (string)GetValue(TranslationProperty); }
            set { SetValue(TranslationProperty, value); }
        }

        IMapData Data { get { return DataContext as IMapData; } }

        Style addBtn;
        Style showBtn;

        public static readonly DependencyProperty NewValueProperty = DependencyProperty.Register(
          nameof(NewValue)
          ,typeof(string)
          ,typeof(SampleView)
          ,new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnValueChanged))
        );

        public string NewValue
        {
            get { return (string)GetValue(NewValueProperty); }
            set
            {
                SetValue(NewValueProperty, value);
                Update();
            }
        }

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value)
            ,typeof(IMapRecord)
            ,typeof(SampleView)
            ,new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnValueChanged))
        );

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as SampleView)?.Update();
        }

        public Core.IMapRecord Value
        {
            get { return (Core.IMapRecord)GetValue(ValueProperty); }
            set
            {
                SetValue(NewValueProperty, value);
                Update();
            }
        }

        delegate int StringProc(int idx);

        static Geometry apply = Geometry.Parse("F1 M 24.3042,23.9875L 31.2708,30.5582L 43.4624,16.6251L 46.7874,19.5542L 31.6667,38L 20.9792,27.3125L 24.3042,23.9875 Z M 16,17L 40.75,17L 38,20L 19,20L 19,40L 38,40L 38,33L 41,29.25L 41,43L 16,43L 16,17 Z M 51.854,35.8737L 59.7707,43.7903L 40.7708,62.7902L 32.8541,54.8736L 51.854,35.8737 Z M 61.2745,42.2067L 53.4374,34.3696L 56.7962,31.0108C 58.0329,29.7742 60.0379,29.7742 61.2746,31.0108L 64.6333,34.3696C 65.87,35.6062 65.87,37.6113 64.6333,38.8479L 61.2745,42.2067 Z M 30.875,65.5609L 30.0833,64.7693L 32.1614,58.1391L 37.5052,63.4829L 30.875,65.5609 Z");
        static Geometry show = Geometry.Parse("F1 M 38,33.1538C 40.6765,33.1538 42.8462,35.3235 42.8462,38C 42.8462,40.6765 40.6765,42.8461 38,42.8461C 35.3235,42.8461 33.1539,40.6765 33.1539,38C 33.1539,35.3235 35.3236,33.1538 38,33.1538 Z M 38,25.0769C 49.3077,25.0769 59,33.1538 59,38C 59,42.8461 49.3077,50.9231 38,50.9231C 26.6923,50.9231 17,42.8461 17,38C 17,33.1538 26.6923,25.0769 38,25.0769 Z M 38,29.1154C 33.0932,29.1154 29.1154,33.0932 29.1154,38C 29.1154,42.9068 33.0932,46.8846 38,46.8846C 42.9068,46.8846 46.8846,42.9068 46.8846,38C 46.8846,33.0932 42.9068,29.1154 38,29.1154 Z");

        static Regex linesRegex = new Regex(@"(?ixmn)(?<sl>^).*?(?<el>$)");
        static Thickness expanderMargin = new Thickness(5);
        static Thickness lineNumPadding = new Thickness() { Left = 5, Right = 10 };
        static Thickness stackMargin = new Thickness() { Left = 10 };
        static SolidColorBrush numLineBrush = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0));

        void AddLine(Grid grid, int number, string value, Brush fore, Brush numFore)
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

        void AddLine(StackPanel nums, StackPanel vals, int number, string value, Brush fore, Brush numFore)
        {
            nums.Children.Add(new TextBlock() { Text = number.ToString(), Padding = lineNumPadding, Background = numLineBrush, Foreground = numFore });
            vals.Children.Add(new TextBlock() { Text = value, Foreground = fore });
        }

        void Update()
        {
            container.Children.Clear();
            var val = Value;
            if (val == null || Data == null)
                return;
            bool noChanges = val.Value == NewValue;
            string code = Data.Text;
            var previwCount = MainVM.Instance.PreviewLines / 2;

            var matches = linesRegex.Matches(code);
            var ls = new List<int>(matches.Count);
            var ll = new List<int>(matches.Count);
            foreach (Match m in matches)
            {
                var idx = m.Groups["sl"].Index;
                ls.Add(idx);
                ll.Add(m.Groups["el"].Index - idx - 1);
            }

            foreach (IMapValueItem itm in Data.Items)
                if (itm != null)
                {
                    Tuple<int, int, string> itemdata = null;
                    if (itm.Value == val.Value)
                    {
                        var grid = new Grid() { VerticalAlignment = VerticalAlignment.Stretch };
                        grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
                        grid.ColumnDefinitions.Add(new ColumnDefinition() /*{ Width = GridLength.Auto }*/);
                        var nums = new StackPanel();
                        Grid.SetColumn(nums, 0);
                        var vals = new StackPanel();
                        var sb = new ScrollViewer() { Content = vals, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled };
                        Grid.SetColumn(sb, 1);
                        grid.Children.Add(nums);
                        grid.Children.Add(sb);

                        var lIdx = ls.BinarySearch(itm.Start);
                        var lIdx2 = ls.BinarySearch(itm.End);
                        if (lIdx < 0) lIdx = ~lIdx - 1;
                        if (lIdx2 < 0) lIdx2 = ~lIdx2 - 1;
                        var lastLine = Math.Min(lIdx2 + previwCount, ls.Count - 1);
                        for (var line = Math.Max(lIdx - previwCount, 0); line <= lastLine; line++)
                        {
                            var brush = (line >= lIdx && line <= lIdx2) ? Brushes.Green : Brushes.Black;
                            AddLine(nums, vals, line + 1, ll[line] > 0 ? code.Substring(ls[line], ll[line]) : string.Empty, brush, brush);
                            if (!noChanges && line == lIdx2)
                            {
                                var rIdx = ls[lIdx];
                                var rLen = ls[lIdx2] + ll[lIdx2] - ls[lIdx];
                                string[] lines = code.Substring(rIdx, rLen).
                                        Remove(itm.EditStart - ls[lIdx], itm.EditEnd - itm.EditStart).
                                        Insert(itm.EditStart - ls[lIdx], itm.NewValue(NewValue)).Replace("\r\n", "\n").Split('\n');
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
                                itemdata = new Tuple<int, int, string>(rIdx, rLen, string.Join("\r\n", lines));
                            }
                        }

                        var stack = new StackPanel() { HorizontalAlignment = HorizontalAlignment.Right, Orientation = Orientation.Horizontal };
                        if (!noChanges)
                        {
                            var btnAdd = new Button() { Style = addBtn, DataContext = itemdata, Content = new System.Windows.Shapes.Path() { Data = apply, Fill = Brushes.Black, Stretch = Stretch.Uniform } };
                            btnAdd.Click += Btn_Click;
                            stack.Children.Add(btnAdd);
                        }
                        var btnShow = new Button() { Style = showBtn, DataContext = itm, Content = new System.Windows.Shapes.Path() { Data = show, Fill = Brushes.Black, Stretch = Stretch.Uniform } };
                        btnShow.Click += BtnShow_Click;
                        stack.Children.Add(btnShow);
                        var header = lIdx == lIdx2 ? string.Format("line: {0}", lIdx + 1) : string.Format("lines: {0} - {1}", lIdx + 1, lIdx2 + 1);
                        stack.Children.Add(new TextBlock()
                        {
                            Text = header,
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = stackMargin
                        });
                        container.Children.Add(new Expander()
                        {
                            Header = stack,
                            Content = grid,
                            IsExpanded = MainVM.Instance.ExpandedPreviews,
                            Background = Brushes.LightYellow,
                            Margin = expanderMargin
                        });
                    }
                }
        }

        private void BtnShow_Click(object sender, RoutedEventArgs e)
        {
            MainVM.Instance.Selected = Data as PathContainer;
            MainVM.Instance.ShowValue(((Button)sender).DataContext as IMapItemRange);
        }

        private void Btn_Click(object sender, RoutedEventArgs e)
        {
            var itm = ((Button)sender).DataContext as Tuple<int, int, string>;
            //если запись уже есть, то проверим одинаковы ли переводы, и если они разные спросим как быть
            //иначе создадим новую запись и зададим её перевод
            if (MappedData.IsValueExists(NewValue))
            {
                var mapItm = (IMapRecordFull)MappedData.GetValueRecord(NewValue);
                if(string.IsNullOrWhiteSpace(mapItm.Translation))
                    mapItm.Translation = Translation;
                else if (!string.IsNullOrWhiteSpace(mapItm.Translation) && !string.Equals(mapItm.Translation, Translation))
                    switch (MessageBox.Show(
                        $"Значение:\r\n\"{mapItm.Value}\"\r\nТекущий перевод:\r\n\"{mapItm.Translation}\"\r\nНовый перевод:\r\n\"{Translation}\"\r\n\r\nЗаменить перевод?"
                        , "Редактирование"
                        , MessageBoxButton.YesNoCancel
                        , MessageBoxImage.Question
                        ))
                    {
                        case MessageBoxResult.Cancel: return;
                        case MessageBoxResult.Yes: mapItm.Translation = Translation; break;
                    }
            }
            else
                ((IMapRecordFull)MappedData.GetValueRecord(NewValue)).Translation = Translation;

            Data.SaveText(Data.Text.Remove(itm.Item1, itm.Item2).Insert(itm.Item1, itm.Item3));
        }
    }
}
