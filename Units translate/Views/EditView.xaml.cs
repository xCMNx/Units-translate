﻿using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Core;
using Ui;

namespace Units_translate.Views
{
    /// <summary>
    /// Логика взаимодействия для EditView.xaml
    /// </summary>
    public partial class EditView
    {
        public ICommand MenuCommand { get; private set; }
        public EditView()
        {
            InitializeComponent();
            MenuCommand = new Command(prop =>
            {
                var val = prop as IMapValueRecord;
                tbValue.SetCurrentValue(TextBox.TextProperty, val.Value);
                tbTranslation.SetCurrentValue(TextBox.TextProperty, val.Translation);
            });
        }

        private void dataList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //var item = dataList.SelectedItem as PathContainer;
            //if (e.ChangedButton == MouseButton.Left && item != null)
            //    (DataContext as MainVM).Selected = item;
        }

        void Undo() => tbTranslation.Text = MainVM.Instance.SelectedValue?.Translation;

        void Apply()
        {
            if (MainVM.Instance.SelectedValue != null)
                MainVM.Instance.SelectedValue.Translation = tbTranslation.Text;
        }

        private void btnUndoTranslation_Click(object sender, System.Windows.RoutedEventArgs e) => Undo();

        private void btnChangeTranslation_Click(object sender, System.Windows.RoutedEventArgs e) => Apply();

        private void tbTranslation_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                switch (e.Key)
                {
                    case Key.Enter: Apply(); e.Handled = true; break;
                    //case Key.Escape: Undo(); e.Handled = true; break;
                }
            }
        }

        private void btnShowInExplorer_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            string filePath = ((sender as FrameworkElement).DataContext as IMapData).FullPath;
            if (File.Exists(filePath))
                System.Diagnostics.Process.Start("explorer.exe", @"/select, " + filePath);
        }

        private void btnOpenFile_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(((sender as FrameworkElement).DataContext as IMapData).FullPath);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            InputManager.Current.ProcessInput(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Right) { RoutedEvent = Mouse.MouseUpEvent, Source = sender });
        }
    }
}
