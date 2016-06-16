using System.Windows.Controls;
using System.Windows.Input;
using Core;

namespace Units_translate.Views
{
    /// <summary>
    /// Логика взаимодействия для TranslatesView.xaml
    /// </summary>
    public partial class TranslatesView : UserControl
    {
        public TranslatesView()
        {
            InitializeComponent();
        }

        private void ListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            FileContainer p = null;
            var itm = (sender as ListView).SelectedItem as Core.IMapRecordFull;
            if (itm != null && itm.Data != null && itm.Data.Count > 0)
                p = itm.Data[0] as FileContainer;
            MainVM.Instance.Selected = p;
        }

        private void ListView_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Delete && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                var lv = sender as ListView;
                if (lv != null)
                {
                    var itm = lv.SelectedItem as IMapValueRecord;
                    if (itm != null)
                        (DataContext as MainVM).Translations.Remove(itm);
                }
            }
        }

        protected void SelectCurrentItem(object sender, KeyboardFocusChangedEventArgs e)
        {
            ListViewItem item = (ListViewItem)sender;
            translatesList.SelectedItem = item.DataContext;
            //item.IsSelected = true;
        }
    }
}
