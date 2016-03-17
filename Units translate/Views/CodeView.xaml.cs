using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
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
        System.Timers.Timer toolTipCloseTimer = new System.Timers.Timer(1000);

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
        }

        private void ContentTextEdit_PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                toolTip.IsOpen = false;
            else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && e.Key == Key.Enter)
            {
                (ContentTextEdit.Tag as IMapValueRecord).Translation = ContentTextEdit.Text;
                toolTip.IsOpen = false;
            }
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
            toolTip.IsOpen = false;
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
            ContentTextEdit.Text = str;
            ContentTextEdit.Tag = mapItm;
            ContentText.Text = str;
            toolBrd.Child = string.IsNullOrWhiteSpace(str) ? EmptyText : ContentText;
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