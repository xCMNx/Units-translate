using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Core;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace Units_translate.Views
{
    /// <summary>
    /// Логика взаимодействия для CodeView.xaml
    /// </summary>
    public partial class CodeView : UserControl
    {
        CustomColorizer colorizer = new CustomColorizer();

        public CodeView()
        {
            InitializeComponent();
            code.TextArea.TextView.LineTransformers.Add(colorizer);
            MainVM.Instance.ShowValueQuery += Instance_ShowValueQuery;
        }

        private void Instance_ShowValueQuery(IMapItem obj)
        {
            if(colorizer.Data != null && obj != null)
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
                    Goto(data.Items.FirstOrDefault(it => it.Value == main.SelectedValue.Value));
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
                    var item = colorizer.Data.ItemAt(code.CaretOffset);
                    if (item != null)
                    {
                        main.SelectedValue = Core.MappedData.GetValueRecord(item.Value, item.ItemType) as IMapRecordFull;
                        return;
                    }
                }
                main.SelectedValue = null;
            }
        }

        void Goto(IMapItem item)
        {
            if (item != null)
            {
                code.CaretOffset = item.ValueStart;
                code.Select(item.ValueStart, item.ValueEnd - item.ValueStart);
                code.ScrollToLine(code.Document.GetLineByOffset(item.ValueStart).LineNumber);
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Goto(((ComboBox)sender).SelectedItem as IMapItem);
        }

        private void btnUpdateFile_Click(object sender, RoutedEventArgs e)
        {
            if(colorizer.Data != null)
                MappedData.UpdateData(colorizer.Data.FullPath, true, false);
        }
    }

    public class CustomColorizer : DocumentColorizingTransformer
    {
        public IMapData Data = null;

        static Action<VisualLineElement> _comment = (VisualLineElement element) => element.TextRunProperties.SetForegroundBrush(Brushes.Green);
        static Action<VisualLineElement> _guid = (VisualLineElement element) => element.TextRunProperties.SetForegroundBrush(Brushes.Gray);
        static Action<VisualLineElement> _directive = (VisualLineElement element) => element.TextRunProperties.SetForegroundBrush(Brushes.LightBlue);
        static Action<VisualLineElement> _string = (VisualLineElement element) => element.TextRunProperties.SetForegroundBrush(Brushes.Blue);
        static Action<VisualLineElement> _stringNoTrans = (VisualLineElement element) =>
        {
            element.TextRunProperties.SetForegroundBrush(Brushes.Blue);
            element.TextRunProperties.SetBackgroundBrush(Brushes.LightPink);
        };
        static Action<VisualLineElement> _none = (VisualLineElement element) => element.TextRunProperties.SetForegroundBrush(Brushes.Red);

        static Action<VisualLineElement> ValueTypeToAction(IMapItem item)
        {
            switch (item.ItemType)
            {
                case MapItemType.Commentary: return _comment;
                case MapItemType.Interface: return _guid;
                case MapItemType.Directive: return _directive;
                case MapItemType.String:
                    {
                        if (string.IsNullOrWhiteSpace(((IMapRecordFull)MappedData.GetValueRecord(item.Value, MapItemType.String)).Translation))
                            return _stringNoTrans;
                        return _string;
                    }
            }
            return _none;
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            if (Data == null || Data.Items == null)
                return;
            int start = line.Offset;
            int end = line.EndOffset;
            var items = Data.ItemsBetween(start, end);
            foreach (var item in items)
                ChangeLinePart(Math.Max(start, item.Start), Math.Min(item.End, end), ValueTypeToAction(item));
        }
    }
}
