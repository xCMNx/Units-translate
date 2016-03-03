using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Core;

namespace Units_translate.Views
{
    /// <summary>
    /// Логика взаимодействия для CodeView.xaml
    /// </summary>
    public partial class CodeView : UserControl
    {
        CustomColorizer colorizer = new CustomColorizer();
        Popup toolTip = new Popup() { Placement = PlacementMode.Mouse };
        Border toolBrd = new Border() { BorderBrush = Brushes.Black, BorderThickness = new Thickness(1), Background = Brushes.White, Padding = new Thickness(2) };
        TextBlock EmptyText = new TextBlock() { Text = "Нет перевода", Foreground = Brushes.Red };
        TextBlock ContentText = new TextBlock();
        TextBox ContentTextEdit = new TextBox() { MinWidth = 150, MaxWidth = 600, MaxHeight = 400, AcceptsReturn = true, AcceptsTab = true, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto};
        System.Timers.Timer toolTipCloseTimer = new System.Timers.Timer(500);
        //static Geometry glphEdit = Geometry.Parse("F1 M 53.2929,21.2929L 54.7071,22.7071C 56.4645,24.4645 56.4645,27.3137 54.7071,29.0711L 52.2323,31.5459L 44.4541,23.7677L 46.9289,21.2929C 48.6863,19.5355 51.5355,19.5355 53.2929,21.2929 Z M 31.7262,52.052L 23.948,44.2738L 43.0399,25.182L 50.818,32.9601L 31.7262,52.052 Z M 23.2409,47.1023L 28.8977,52.7591L 21.0463,54.9537L 23.2409,47.1023 Z M 17,28L 17,23L 23,23L 23,17L 28,17L 28,23L 34,23L 34,28L 28,28L 28,34L 23,34L 23,28L 17,28 Z");
        //UserControl tooltipContent = new UserControl();
        public CodeView()
        {
            InitializeComponent();
            code.TextArea.TextView.LineTransformers.Add(colorizer);
            MainVM.Instance.ShowValueQuery += Instance_ShowValueQuery;
            toolTip.Child = toolBrd;
            toolTipCloseTimer.Elapsed += (s, e) => Helpers.mainCTX.Send(_ => toolTip.IsOpen = false, null);
            EmptyText.MouseDown += EmptyText_MouseDown;
            ContentText.MouseDown += EmptyText_MouseDown;
            ContentTextEdit.PreviewLostKeyboardFocus += ContentTextEdit_LostFocus;
            ContentTextEdit.PreviewKeyUp += ContentTextEdit_PreviewKeyUp;
        //var grd = new Grid();
        //grd.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(20) });
        //grd.ColumnDefinitions.Add(new ColumnDefinition());
        //var btnEdit = new Button() { Content = new Path() { Data = glphEdit, Stretch = Stretch.Uniform, Fill = Brushes.Black } };
        //grd.Children.Add(btnEdit);
        //grd.Children.Add(tooltipContent);
        //Grid.SetColumn(tooltipContent, 1);
        //toolTip.Content = grd;
        //toolTip.PlacementTarget = this;
        //toolTip.MouseEnter += (s, e) => toolTipCloseTimer.Stop();
        //btnEdit.IsHitTestVisible = true;
        }

        private void ContentTextEdit_PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
                toolTip.IsOpen = false;
        }

        private void ContentTextEdit_LostFocus(object sender, RoutedEventArgs e)
        {
            if (toolTip.IsOpen)
            {
                toolTip.IsOpen = false;
                var itm = ContentTextEdit.Tag as IMapValueRecord;
                if (string.Compare(itm.Translation, ContentTextEdit.Text) != 0 &&
                    MessageBox.Show("Перевод был изменен, принять изменение?", "Переводы", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    itm.Translation = ContentTextEdit.Text;
                }
            }
        }

        private void EmptyText_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            toolTipCloseTimer.Stop();
            toolBrd.Child = ContentTextEdit;
            ContentTextEdit.Focus();
        }

        private void Instance_ShowValueQuery(IMapItemRange obj)
        {
            if (colorizer.Data != null && obj != null)
                Goto(colorizer.Data.Items.FirstOrDefault(it => it == obj));
        }

        private void CodeView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            IMapData data = e.OldValue as IMapData;
            if (data != null)
                data.PropertyChanged -= Data_PropertyChanged;
            data = e.NewValue as IMapData;
            if (data != null && data.Items != null)
            {
                data.PropertyChanged += Data_PropertyChanged;
                colorizer.Data = data;
                code.Text = data.Text;
                code.ScrollToHome();
                var main = DataContext as MainVM;
                if (main.SelectedValue != null)
                    Goto(data.Items.FirstOrDefault(it => it is IMapValueItem && (it as IMapValueItem).Value == main.SelectedValue.Value));
            }
            else
            {
                colorizer.Data = null;
                code.Text = string.Empty;
            }
        }

        private void Data_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IMapData.Items) || e.PropertyName == nameof(IMapData.Text))
                code.Text = colorizer.Data.Text;
        }

        //private void code_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        //{
        //    if (colorizer.Data != null)
        //    {
        //        var cursor = Core.Helpers.GetMousePosition();
        //        var pnt = code.PointFromScreen(new Point(cursor.X, cursor.Y));
        //        var p = code.GetPositionFromPoint(pnt);
        //        if (p.HasValue)
        //        {
        //            IMapItem item = colorizer.Data.ItemAt(code.Document.GetOffset(p.Value.Location));
        //            if (item != null && item.ItemType == MapItemType.String)
        //                return;
        //        }
        //    }
        //    e.Handled = true;
        //}
        private void code_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var main = DataContext as MainVM;
            if (main != null)
            {
                if (colorizer.Data != null)
                {
                    var item = colorizer.Data.ValueItemAt(code.CaretOffset);
                    if (item != null)
                    {
                        main.SelectedValue = Core.MappedData.GetValueRecord(item.Value) as IMapValueRecord;
                        return;
                    }
                }

                main.SelectedValue = null;
            }
        }

        void Goto(IMapItemRange item)
        {
            if (item == null)
                return;
            var iv = item as IMapValueItem;
            if (iv != null)
            {
                code.CaretOffset = iv.EditStart;
                code.Select(iv.EditStart, iv.EditEnd - iv.EditStart);
                code.ScrollToLine(code.Document.GetLineByOffset(iv.EditStart).LineNumber);
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Goto(((ComboBox)sender).SelectedItem as IMapItemRange);
        }

        private void btnUpdateFile_Click(object sender, RoutedEventArgs e)
        {
            if (colorizer.Data != null)
                MappedData.UpdateData(colorizer.Data.FullPath, true, false);
        }

        private void code_MouseHover(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (colorizer.Data == null || (toolTip.IsOpen && toolBrd.Child == ContentTextEdit))
                return;
            var pos = code.GetPositionFromPoint(e.GetPosition(code));
            if (!pos.HasValue)
                return;
            var item = colorizer.Data.ValueItemAt(code.Document.GetOffset(pos.Value.Location));
            if (item == null)
                return;
            var mapItm = MappedData.GetValueRecord(item.Value) as IMapValueRecord;
            if (mapItm == null)
                return;
            toolTipCloseTimer.Stop();
            var str = mapItm.Translation;
            ContentText.Text = str;
            ContentTextEdit.Text = str;
            ContentTextEdit.Tag = mapItm;
            toolBrd.Child = string.IsNullOrWhiteSpace(str) ? EmptyText : ContentText;
            //toolTip.Content = string.IsNullOrWhiteSpace(str) ? EmptyText : (object)str;
            //tooltipContent.Content = string.IsNullOrWhiteSpace(str) ? EmptyText : (object)str;
            toolTip.IsOpen = true;
            e.Handled = true;
        }

        private void code_MouseHoverStopped(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (toolTip.IsOpen && toolBrd.Child == ContentTextEdit)
                return;
            toolTipCloseTimer.Start();
        }
    }
}