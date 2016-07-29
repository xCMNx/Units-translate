using System;
using System.Linq;
using System.Text.RegularExpressions;
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

        public int FindNext(IMapValueRecord[] lst, Regex rgxp, int firstIdx = 0)
        {
            try
            {
                for (var i = firstIdx; i < lst.Length; i++)
                    if (rgxp.IsMatch(lst[i].Value) || rgxp.IsMatch(lst[i].Translation))
                        return i;
            }
            catch
            {
            }
            return -1;
        }

        public int FindNext(IMapValueRecord[] lst, string pattern, int firstIdx = 0)
        {
            try
            {
                var rgxp = new Regex(pattern, RegexOptions.IgnoreCase, new System.TimeSpan(0, 0, 1));
                return FindNext(lst, rgxp, firstIdx);
            }
            catch
            {
            }
            return -1;
        }

        public IMapValueRecord FindNext(IMapValueRecord[] lst, string pattern, IMapValueRecord curr = null)
        {
            try
            {
                var rgxp = new Regex(pattern, RegexOptions.IgnoreCase, new System.TimeSpan(0, 0, 1));
                var idx = FindNext(lst, rgxp, curr != null && (rgxp.IsMatch(curr.Value) || !rgxp.IsMatch(curr.Translation)) ? Array.IndexOf(lst, curr) + 1 : 0);
                if (idx > -1)
                    return lst[idx];
            }
            catch
            {
            }
            return curr;
        }

        private void searchText_TextChanged(object sender, TextChangedEventArgs e)
        {
            var vals = MainVM.Instance.Translates.OfType<IMapValueRecord>().ToArray();
            translatesList.SelectedItem = FindNext(vals, searchText.Text, null);
            translatesList.ScrollIntoView(translatesList.SelectedItem);
        }

        private void btnFindNext_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var vals = MainVM.Instance.Translates.OfType<IMapValueRecord>().ToArray();
            translatesList.SelectedItem = FindNext(vals, searchText.Text, translatesList.SelectedItem as IMapValueRecord);
            translatesList.ScrollIntoView(translatesList.SelectedItem);
        }
    }
}
