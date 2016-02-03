using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Core;
using static Units_translate.Views.CodePreviewBuilder;

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
          , typeof(string)
          , typeof(SampleView)
          , new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnValueChanged))
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

        public static readonly DependencyProperty FormatProperty = DependencyProperty.Register(
          nameof(Format)
          , typeof(string)
          , typeof(SampleView)
          , new FrameworkPropertyMetadata("{0}", new PropertyChangedCallback(OnValueChanged))
        );

        public string Format
        {
            get { return (string)GetValue(FormatProperty); }
            set
            {
                SetValue(FormatProperty, value);
                Update();
            }
        }

        public static readonly DependencyProperty UseFormatProperty = DependencyProperty.Register(
          nameof(UseFormat)
          , typeof(bool)
          , typeof(SampleView)
          , new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnValueChanged))
        );

        public bool UseFormat
        {
            get { return (bool)GetValue(UseFormatProperty); }
            set
            {
                SetValue(UseFormatProperty, value);
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
        static Geometry wrap = Geometry.Parse("F1 M 37.3,50L 31.9943,50L 28.5748,40.8812C 28.447,40.5437 28.3134,39.9188 28.174,39.0062L 28.1174,39.0062L 27.66,40.9625L 24.2275,50L 18.9,50L 25.225,36L 19.4576,22L 24.8896,22L 27.721,30.3938C 27.9417,31.0604 28.1392,31.8479 28.3134,32.7563L 28.3701,32.7563L 28.9843,30.3188L 32.1032,22L 37.0212,22L 31.1797,35.8813L 37.3,50 Z M 44.5466,58.0533L 42.7733,58.0533C 40.24,55.1321 38.9733,51.53 38.9733,47.2471C 38.9733,42.9483 40.24,39.2882 42.7733,36.2667L 44.5466,36.2667C 42.0133,39.4017 40.7467,43.0539 40.7467,47.2233C 40.7467,51.3558 42.0133,54.9658 44.5466,58.0533 Z M 59.7267,58.0533L 57.9533,58.0533C 60.4867,54.9658 61.7533,51.3558 61.7533,47.2233C 61.7533,43.0539 60.4867,39.4017 57.9533,36.2667L 59.7267,36.2667C 62.26,39.2882 63.5267,42.9483 63.5267,47.2471C 63.5267,51.53 62.26,55.1321 59.7267,58.0533 Z M 58.84,39.3333L 53.7298,53.1083C 52.5106,56.4228 50.6753,58.08 48.2237,58.08C 47.2896,58.08 46.5217,57.9111 45.92,57.5733L 45.92,54.5333C 46.4267,54.8711 46.9769,55.04 47.5706,55.04C 48.5496,55.04 49.2318,54.5637 49.6171,53.611L 50.2662,52.0317L 45.16,39.3333L 49.4865,39.3333L 51.8179,47.0838C 51.963,47.5667 52.0765,48.1354 52.1583,48.7898L 52.2058,48.7898L 52.6096,47.1115L 54.9648,39.3333L 58.84,39.3333 Z");

        static Thickness expanderMargin = new Thickness(5);
        static Thickness stackMargin = new Thickness() { Left = 10 };

        void Update()
        {
            container.Children.Clear();
            var val = Value;
            if (val == null || Data == null)
                return;
            bool noChanges = val.Value == NewValue;
            bool filterEmpty = true;
            if (UseFormat)
            {
                try
                {
                    filterEmpty = string.IsNullOrWhiteSpace(string.Format(Format, string.Empty));
                }
                catch { }
            }
            var previwCount = MainVM.Instance.PreviewLines / 2;
            var map = new PreviewMap(Data.Text);

            foreach (var item in Data.Items)
            {
                var itm = item as IMapValueItem;
                if (itm != null && itm.Value == val.Value)
                {
                    string nVal = string.Empty;
                    try
                    {
                        nVal = string.Format(UseFormat ? Format : "{0}", noChanges ? UseFormat ? itm.NewValue(itm.Value) : string.Empty : itm.NewValue(NewValue));
                    }
                    catch { }
                    var previewData = MakePreview(map, itm, previwCount, nVal);
                    var stack = new StackPanel() { HorizontalAlignment = HorizontalAlignment.Right, Orientation = Orientation.Horizontal };
                    if (!noChanges || !filterEmpty)
                    {
                        var btnAdd = new Button() { Style = addBtn, DataContext = previewData.Item2, Content = new System.Windows.Shapes.Path() { Data = apply, Fill = Brushes.Black, Stretch = Stretch.Uniform } };
                        btnAdd.Click += Btn_Click;
                        stack.Children.Add(btnAdd);
                    }
                    var btnShow = new Button() { Style = showBtn, DataContext = itm, Content = new System.Windows.Shapes.Path() { Data = show, Fill = Brushes.Black, Stretch = Stretch.Uniform } };
                    btnShow.Click += BtnShow_Click;
                    stack.Children.Add(btnShow);

                    var lIdx = map.LineIndexAt(itm.Start);
                    var lIdx2 = map.LineIndexAt(itm.End);
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
                        Content = previewData.Item1,
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
                var mapItm = (IMapValueRecord)MappedData.GetValueRecord(NewValue);
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
                ((IMapValueRecord)MappedData.GetValueRecord(NewValue)).Translation = Translation;

            Data.SaveText(Data.Text.Remove(itm.Item1, itm.Item2).Insert(itm.Item1, itm.Item3));
        }
    }
}
