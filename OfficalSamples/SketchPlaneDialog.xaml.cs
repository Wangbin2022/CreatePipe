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
    /// SketchPlaneDialog.xaml 的交互逻辑
    /// </summary>
    public partial class SketchPlaneDialog : Window
    {
        private readonly SketchPlaneDialogViewModel _viewModel;
        public SketchPlaneCreationParams CreationParams => _viewModel.CreationParams;
        public SketchPlaneDialog()
        {
            InitializeComponent();
            _viewModel = new SketchPlaneDialogViewModel();
            _viewModel.CloseRequest += result =>
            {
                DialogResult = result;
                Close();
            };

            DataContext = _viewModel;
        }
    }
    /// <summary>
    /// 草图平面创建对话框视图模型
    /// </summary>
    public class SketchPlaneDialogViewModel : ObserverableObject
    {
        public SketchPlaneCreationParams CreationParams { get; } = new SketchPlaneCreationParams();

        public ICommand OkCommand { get; }
        public ICommand CancelCommand { get; }

        public event Action<bool> CloseRequest;

        public SketchPlaneDialogViewModel()
        {
            OkCommand = new BaseBindingCommand(_ => CloseRequest?.Invoke(true));
            CancelCommand = new BaseBindingCommand(_ => CloseRequest?.Invoke(false));
        }
    }
}
