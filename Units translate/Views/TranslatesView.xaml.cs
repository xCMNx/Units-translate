using System.Windows.Controls;
using Core;
using Microsoft.Win32;

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

        private void btnUpdate_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            (DataContext as MainVM).UpdateTranslatesEntries();
        }

        private void ListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            PathContainer p = null;
            var itm = (sender as ListView).SelectedItem as Core.IMapRecordFull;
            if (itm != null && itm.Data != null && itm.Data.Count > 0)
                p = itm.Data[0] as FileContainer;
            MainVM.Instance.Selected = p;
        }

        private void btnSave_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var sd = new SaveFileDialog();
            sd.Filter = "XML|*.xml";
            if (sd.ShowDialog().Value == true)
                (DataContext as MainVM).SaveTranslationsNew(sd.FileName);
        }

        private void btnDelEmpty_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var lst = (DataContext as MainVM).Translations;
            for (int i = lst.Count - 1; i >= 0; i--)
                if (string.IsNullOrWhiteSpace(lst[i].Translation))
                    lst.RemoveAt(i);
        }

        private void btnDelUnAttouched_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var lst = (DataContext as MainVM).Translations;
            for (int i = lst.Count - 1; i >= 0; i--)
                if (lst[i].Data.Count == 0)
                    lst.RemoveAt(i);
        }

        private void ListView_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Delete)
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
    }
}
