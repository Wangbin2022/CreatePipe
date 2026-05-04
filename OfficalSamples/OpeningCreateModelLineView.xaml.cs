using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// OpeningCreateModelLineView.xaml 的交互逻辑
    /// </summary>
    public partial class OpeningCreateModelLineView : Window
    {
        public OpeningCreateModelLineView()
        {
            InitializeComponent();
        }
    }
    /// <summary>
    /// 创建模型线选项ViewModel - 使用C# 7.3表达式体成员
    /// </summary>
    public class OpeningCreateModelLineViewModel : ObserverableObject
    {
        private CreationScope _selectedScope = CreationScope.DisplayedOpening;

        public CreationScope SelectedScope
        {
            get => _selectedScope;
            set { _selectedScope = value; OnPropertyChanged(); }
        }

        public bool IsAllOpeningsSelected
        {
            get => _selectedScope == CreationScope.AllOpenings;
            set { if (value) SelectedScope = CreationScope.AllOpenings; }
        }

        public bool IsShaftOpeningsSelected
        {
            get => _selectedScope == CreationScope.ShaftOpeningsOnly;
            set { if (value) SelectedScope = CreationScope.ShaftOpeningsOnly; }
        }

        public bool IsDisplayedOpeningSelected
        {
            get => _selectedScope == CreationScope.DisplayedOpening;
            set { if (value) SelectedScope = CreationScope.DisplayedOpening; }
        }

        public ICommand CreateCommand { get; }
        public ICommand CancelCommand { get; }

        public OpeningCreateModelLineViewModel()
        {
            CreateCommand = new BaseBindingCommand(_ => OnCreate());
            CancelCommand = new BaseBindingCommand(_ => CloseWindow?.Invoke());
        }

        private void OnCreate()
        {
            CreationScopeSelected?.Invoke(_selectedScope);
            CloseWindow?.Invoke();
        }

        public event Action<CreationScope> CreationScopeSelected;
        public Action CloseWindow { get; set; }
    }
    /// <summary>
    /// 创建模型线选项枚举
    /// </summary>
    public enum CreationScope
    {
        AllOpenings,        // 所有洞口
        ShaftOpeningsOnly,  // 仅竖井洞口
        DisplayedOpening    // 当前显示的洞口
    }
}
