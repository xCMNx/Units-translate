using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Core;
using Ui;

namespace Units_translate.Views
{
    /// <summary>
    /// Логика взаимодействия для EditView.xaml
    /// </summary>
    public partial class EditView
    {
        public ICommand MenuCommand { get; private set; }
        public EditView()
        {
            InitializeComponent();
            MenuCommand = new Command(prop =>
            {
                var val = prop as IMapValueRecord;
                tbValue.SetCurrentValue(TextBox.TextProperty, val.Value);
                tbTranslation.SetCurrentValue(TextBox.TextProperty, val.Translation);
            });
        }

        private void dataList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //var item = dataList.SelectedItem as PathContainer;
            //if (e.ChangedButton == MouseButton.Left && item != null)
            //    (DataContext as MainVM).Selected = item;
        }

        void Undo() => tbTranslation.SetCurrentValue(TextBox.TextProperty, MainVM.Instance.SelectedValue?.Translation);

        void Apply()
        {
            if (MainVM.Instance.SelectedValue != null)
                MainVM.Instance.SelectedValue.Translation = tbTranslation.Text;
        }

        private void btnUndoTranslation_Click(object sender, System.Windows.RoutedEventArgs e) => Undo();

        private void btnChangeTranslation_Click(object sender, System.Windows.RoutedEventArgs e) => Apply();

        private void tbTranslation_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                switch (e.Key)
                {
                    case Key.Enter: Apply(); e.Handled = true; break;
                }
            }
            else
            {
                switch (e.Key)
                {
                    case Key.Escape: Undo(); e.Handled = true; break;
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            InputManager.Current.ProcessInput(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Right) { RoutedEvent = Mouse.MouseUpEvent, Source = sender });
        }

        async static Task<string> tr(string str, string srcCode, string dstCode)
        {
            var src = srcCode?.Split('_')[0];
            var dst = dstCode?.Split('_');
            return await MainVM.TranslateText(str, src, dst[0], dst.Length > 1 && dst[1].StartsWith("lat", StringComparison.InvariantCultureIgnoreCase));
        }

        private async void btnValueTrans_ToolTipOpening(object sender, ToolTipEventArgs e)
        {
            if ((string)btnValueTr.Tag != tbValue.Text || (string)cbValueLang.Tag != cbValueLang.Text || (string)cbTransLang.Tag != cbTransLang.Text)
            {
                btnValueTr.ToolTip = "...";
                btnValueTr.ToolTip = await tr(tbValue.Text, cbValueLang.Text, cbTransLang.Text);
                btnValueTr.Tag = tbValue.Text;
                cbValueLang.Tag = cbValueLang.Text;
            }
        }

        private async void btnTransTr_ToolTipOpening(object sender, ToolTipEventArgs e)
        {
            if ((string)btnTransTr.Tag != tbTranslation.Text || (string)cbValueLang.Tag != cbValueLang.Text || (string)cbTransLang.Tag != cbTransLang.Text)
            {
                btnTransTr.ToolTip = "...";
                btnTransTr.ToolTip = await tr(tbTranslation.Text, cbTransLang.Text, cbValueLang.Text);
                btnTransTr.Tag = tbTranslation.Text;
                cbTransLang.Tag = cbTransLang.Text;
            }
        }

        private void btnTransTr_Click(object sender, RoutedEventArgs e)
        {
            if ((string)btnTransTr.Tag == tbTranslation.Text)
                Clipboard.SetText((string)btnTransTr.ToolTip);
        }

        private void btnValueTr_Click(object sender, RoutedEventArgs e)
        {
            if ((string)btnValueTr.Tag == tbValue.Text)
                Clipboard.SetText((string)btnValueTr.ToolTip);
        }

        private void btnSwitchTrAndValue_Click(object sender, RoutedEventArgs e)
        {
            var tr = tbTranslation.Text;
            tbTranslation.SetCurrentValue(TextBox.TextProperty, tbValue.Text);
            tbValue.SetCurrentValue(TextBox.TextProperty, tr);
        }
    }
}
