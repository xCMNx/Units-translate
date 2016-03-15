using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Core;
using Microsoft.Win32;

namespace Units_translate
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MainVM mainVm = MainVM.Instance;
        static string LAST_PATH = "LastPath";

        public MainWindow()
        {
            InitializeComponent();
            DataContext = mainVm;
            lastPath.Text = Core.Helpers.ReadFromConfig(LAST_PATH, null);
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            main.Visibility = Visibility.Collapsed;
            settings.Visibility = Visibility.Visible;
        }

        void SwitchSettings()
        {
            main.Visibility = Visibility.Visible;
            settings.Visibility = Visibility.Collapsed;
            mainVm.NotifyPropertiesChanged(nameof(MainVM.IgnoreText), nameof(MainVM.WriteEncoding), nameof(MainVM.ReadEncoding), nameof(MainVM.UseWriteEncoding));
        }

        private void Button_Settings_Ok_Click(object sender, RoutedEventArgs e)
        {
            mainVm.IgnoreText = settingsView.tbIgnore.Text;
            mainVm.ReadEncoding = (System.Text.Encoding)settingsView.cbReadEncoding.SelectedItem;
            mainVm.WriteEncoding = (System.Text.Encoding)settingsView.cbWriteEncoding.SelectedItem;
            mainVm.UseWriteEncoding = settingsView.chbSaveEncodingChecked.IsChecked == true;
            SwitchSettings();
        }

        private void Button_Settings_Cancel_Click(object sender, RoutedEventArgs e)
        {
            SwitchSettings();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            using (var od = new System.Windows.Forms.FolderBrowserDialog())
            {
                od.SelectedPath = lastPath.Text;
                if (od.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    lastPath.Text = od.SelectedPath;
                    Core.Helpers.WriteToConfig(LAST_PATH, lastPath.Text);
                    Scan();
                }
            }
        }

        void Work(Action<Action<string, int>> method)
        {
            pbGrp.Visibility = Visibility.Visible;
            pb.Value = 0;
            pbLbl.Text = string.Empty;

            var bw = new BackgroundWorker();
            bw.DoWork += (sndr, ev) => method((s, c) =>
                Core.Helpers.mainCTX.Send(_ =>
                {
                    pbLbl.Text = s;
                    if (s == null)
                        pb.Maximum = c;
                    else
                        pb.Value = c;
                }, null)
            );
            bw.RunWorkerCompleted += (sndr, ev) =>
            {
                pbGrp.Visibility = Visibility.Hidden;
                mainVm.ShowTree();
            };
            bw.RunWorkerAsync(lastPath.Text);
        }

        void Scan()
        {
            var path = lastPath.Text;
            if (Directory.Exists(path))
                Work((callback) => mainVm.AddDir(path, callback));
        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            Work((callback) => mainVm.Remap(callback));
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            mainVm.SelectedValue = null;
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            Scan();
        }

        private void lastPath_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
                Scan();
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            var od = new OpenFileDialog();
            od.Filter = "XML|*.xml";
            od.FileName = "eng_rus.xml";
            if (od.ShowDialog().Value == true)
                mainVm.LoadTranslations(od.FileName);
        }

        private void Expander_Collapsed(object sender, RoutedEventArgs e)
        {
            expanderRow.Height = GridLength.Auto;
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            var sd = new SaveFileDialog();
            sd.Filter = "XML|*.xml";
            if (sd.ShowDialog().Value == true)
                mainVm.SaveTranslations(sd.FileName);
        }

        private void ListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            PathContainer p = null;
            var itm = lbTranslates.SelectedItem as Core.IMapRecordFull;
            if (itm != null && itm.Data != null && itm.Data.Count > 0)
                p = itm.Data[0] as FileContainer;
            MainVM.Instance.Selected = p;
        }

    }
}
