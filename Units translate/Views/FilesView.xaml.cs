using System.Diagnostics;
using System.Windows.Input;
using Ui;

namespace Units_translate.Views
{
    /// <summary>
    /// Логика взаимодействия для FilesView.xaml
    /// </summary>
    public partial class FilesView
    {
        public FilesView()
        {
            InitializeComponent();
        }

        private void SelectedItemChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<object> e)
        {
            var data = DataContext as MainVM;
            if (data != null)
                data.Selected = ((TreeListViewItem)e.NewValue)?.Header as FileContainer;
        }

        private void TreeListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                e.Handled = true;
                var item = ((sender as TreeListView).SelectedItem as TreeListViewItem)?.Header as PathBase;
                if (item != null)
                    Process.Start(item.FullPath);
            }
        }
    }
}
