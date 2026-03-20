using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using CreatePipe.Form;
using CreatePipe.Form.Converter;
using CreatePipe.Utils;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CreatePipe
{
    /// <summary>
    /// TestWindow.xaml 的交互逻辑
    /// </summary>
    public partial class TestWindow : Window
    {
        public TestWindow(UIApplication uiApp)
        {
            InitializeComponent();
            this.DataContext = new TestWindowViewModel(uiApp);
        }

        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
    public class TestWindowViewModel : ObserverableObject
    {
        private UIApplication _uiapp;
        private UIDocument _uidoc;
        private Document _doc;
        public TestWindowViewModel(UIApplication uiApp)
        {
            _uiapp = uiApp;
            _uidoc = uiApp.ActiveUIDocument;
            _doc = _uidoc.Document;

        }
        public ICommand SaveConfigCommand => new BaseBindingCommand(SaveConfig);
        private void SaveConfig(object obj)
        {
        }
    }
}
