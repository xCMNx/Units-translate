using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Core;

namespace Units_translate.Views
{
    /// <summary>
    /// Логика взаимодействия для TranslationConflictsView.xaml
    /// </summary>
    public partial class TranslationConflictsView
    {
        public TranslationConflictsView()
        {
            InitializeComponent();
        }

        private void lbTranslatesConflicts_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            PathContainer p = null;
            var itm = ((KeyValuePair<IMapRecordFull, SortedItems<string>>)lbTranslatesConflicts.SelectedItem).Key;
            if (itm != null && itm.Data != null && itm.Data.Count > 0)
                p = itm.Data[0] as FileContainer;
            MainVM.Instance.Selected = p;
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var str = (string)((Button)sender).Tag;
            Clipboard.SetText(str);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var str = (string)((Button)sender).Tag;
            MainVM.Instance.RemoveConflictVariant(str);
            lbTranslatesConflicts.Items.Refresh();
            lbConflictsStrings.Items.Refresh();
        }
    }
}
