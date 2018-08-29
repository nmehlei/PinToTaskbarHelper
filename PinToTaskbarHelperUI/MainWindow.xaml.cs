using System;
using System.Windows;
using Microsoft.Win32;
using PinToTaskbarHelper.Library;
using PinToTaskbarHelperUI.Models;

namespace PinToTaskbarHelperUI
{
    public partial class MainWindow
    {
        // Constructors

        public MainWindow()
        {
            InitializeComponent();
            _model = new MainWindowBindingModel();
            DataContext = _model;
        }

        // Fields

        private readonly MainWindowBindingModel _model;

        // Methods

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Executables (*.exe)"
            };
            if (dlg.ShowDialog() == true)
            {
                _model.IsCreatingShortcut = true;

                try
                {
                    TaskbarPinHelper.PinApplication(dlg.FileName);
                }
                catch (Exception exc)
                {
                    MessageBox.Show("There was an error while creating the taskbar pin!" + Environment.NewLine + Environment.NewLine
                        + exc, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                _model.IsCreatingShortcut = false;
            }
        }
    }
}