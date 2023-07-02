using System.Windows.Controls;
using System.Linq;
using Core;

namespace Units_translate.Views
{
    /// <summary>
    /// Логика взаимодействия для UnitsView.xaml
    /// </summary>
    public partial class UnitsView : UserControl
    {
        public UnitsView()
        {
            InitializeComponent();
        }

        private void doSelect(IMapUnitEntry item)
        {
            MainVM.Instance.Selected = item == null ? null : MainVM.Instance.FilesTree.Files.FirstOrDefault(f => f.FullPath.Equals(item.Path, System.StringComparison.InvariantCultureIgnoreCase));
        }

        private void ListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if ((sender as ListView).SelectedItem is IMapUnitEntry item)
                doSelect(item);
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((sender as ListView).SelectedItem is IMapUnitEntry item)
                MainVM.Instance.FocusedUnit = item;
        }
    }
}
