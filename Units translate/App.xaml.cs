using System.Windows;
using Core;

namespace Units_translate
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Exit(object sender, ExitEventArgs e)
        {
            Helpers.mainCTS.Cancel();
        }
    }
}
