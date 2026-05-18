using CreatePipe.cmd;
using System;
using System.Windows;
using System.Windows.Input;

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
