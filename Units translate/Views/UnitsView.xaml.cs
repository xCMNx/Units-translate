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

        private void ListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if ((sender as ListView).SelectedItem is IMapUnitEntry item)
                MainVM.Instance.Selected = MainVM.Instance.FilesTree.Files.FirstOrDefault(f => f.FullPath.Equals(item.Path, System.StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
