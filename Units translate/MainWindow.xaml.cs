﻿using System;
using System.ComponentModel;
using System.IO;
using System.Windows;

namespace Units_translate
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static string LAST_PATH = "LastPath";

        public MainWindow()
        {
            InitializeComponent();
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
            MainVM.Instance.NotifyPropertiesChanged(nameof(MainVM.IgnoreText), nameof(MainVM.WriteEncoding), nameof(MainVM.ReadEncoding), nameof(MainVM.UseWriteEncoding));
        }

        private void Button_Settings_Ok_Click(object sender, RoutedEventArgs e)
        {
            MainVM.Instance.IgnoreText = settingsView.tbIgnore.Text;
            MainVM.Instance.ReadEncoding = (System.Text.Encoding)settingsView.cbReadEncoding.SelectedItem;
            MainVM.Instance.WriteEncoding = (System.Text.Encoding)settingsView.cbWriteEncoding.SelectedItem;
            MainVM.Instance.UseWriteEncoding = settingsView.chbSaveEncodingChecked.IsChecked == true;
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
            bw.RunWorkerCompleted += (sndr, ev) => pbGrp.Visibility = Visibility.Hidden;
            bw.RunWorkerAsync(lastPath.Text);
        }

        void Scan()
        {
            var path = lastPath.Text;
            if (Directory.Exists(path))
                Work((callback) => MainVM.Instance.AddDir(path, callback));
        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            Work((callback) => MainVM.Instance.Remap(callback));
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            MainVM.Instance.SelectedValue = null;
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

        private void ListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //PathContainer p = null;
            //var itm = lbTranslates.SelectedItem as Core.IMapRecordFull;
            //if (itm != null && itm.Data != null && itm.Data.Count > 0)
            //    p = itm.Data[0] as FileContainer;
            //MainVM.Instance.Selected = p;
        }

        private void btnIgnorTranslateConflicts_Click(object sender, RoutedEventArgs e)
        {
            MainVM.Instance.ClearTranslationConflicts();
            tabs.SelectedItem = tiFiles;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = Core.Translations.IsTranslatesChanged() && MessageBox.Show("Переводы были изменены.\r\nВы уверены, что не хотите сохранить изменения?", "Внимание", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Work(MainVM.Instance.OpenSolution);
        }

        private void btnFix_Click(object sender, RoutedEventArgs e)
        {
            Work((callback) => MainVM.Instance.FixUnitPaths(callback));
        }
    }
}
