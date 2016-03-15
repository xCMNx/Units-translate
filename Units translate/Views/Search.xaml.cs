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
            PathContainer p = null;
            var itm = lbSearchResults.SelectedItem as Core.IMapRecordFull;
            if (itm != null && itm.Data != null && itm.Data.Count > 0)
                p = itm.Data[0] as FileContainer;
            MainVM.Instance.Selected = p;
        }
    }
}
