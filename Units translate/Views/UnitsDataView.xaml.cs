using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;

namespace Units_translate.Views
{
    /// <summary>
    /// Логика взаимодействия для UnitsDataView.xaml
    /// </summary>
    public partial class UnitsDataView : UserControl
    {
        public UnitsDataView()
        {
            InitializeComponent();
        }

        private void TextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(sender is TextBox tb)
            {
                var str = tb.GetLineText(tb.GetLineIndexFromCharacterIndex(tb.GetCharacterIndexFromPoint(e.GetPosition(tb), true))).Trim('\r', '\n');
                var unit = MainVM.Instance.GetUnitsEntries().FirstOrDefault(u => u.ToUnitString().Equals(str));
                MainVM.Instance.ShowDependsFor(unit);
            }
        }
    }
}
