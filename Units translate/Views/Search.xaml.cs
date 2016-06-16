using System.Windows.Input;

namespace Units_translate.Views
{
    /// <summary>
    /// Логика взаимодействия для Search.xaml
    /// </summary>
    public partial class Search
    {
        public Search()
        {
            InitializeComponent();
        }

        private void lbSearchResults_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            FileContainer p = null;
            var itm = lbSearchResults.SelectedItem as Core.IMapRecordFull;
            if (itm != null && itm.Data != null && itm.Data.Count > 0)
                p = itm.Data[0] as FileContainer;
            MainVM.Instance.Selected = p;
        }

        private void tbSearch_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Enter)
                MainVM.Instance.SearchText = tbSearch.Text;
        }
    }
}
