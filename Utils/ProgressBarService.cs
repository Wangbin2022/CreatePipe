using CreatePipe.models;
using System;
using System.Windows.Threading;

namespace CreatePipe.Utils
{
    public class ProgressBarService : IProgressBarService
    {
        ProgressBarDialog _dialog;
        private ProgressBarDialogViewModel _viewModel;
        public void Start(int maximum, string initialTitle = "准备中...")
        {
            // 1. 实例化 ViewModel
            //_viewModel = new ProgressBarDialogViewModel(maximum, initialTitle);
            _viewModel = new ProgressBarDialogViewModel(maximum, initialTitle, 0);
            // 2. 实例化 Window 并传入 ViewModel
            _dialog = new ProgressBarDialog(_viewModel);
            // 3. 【关键点1】必须使用 Show() 开启非模态窗口，不能用 ShowDialog()
            _dialog.Show();
            // 强制刷新一次界面
            RefreshUI();
        }
        // 关键方法：允许中途调整总数
        public void Reset(int newMax, string newTitle)
        {
            if (_viewModel == null) return;
            // 建议回到主线程更新 UI
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                _viewModel.Maximum = newMax;
                _viewModel.Value = 0;
                _viewModel.Title = newTitle;
            });
        }
        public void UpdateMax(int newTotal)
        {
            if (_viewModel != null) _viewModel.Maximum = newTotal;
        }
        public void Update(int currentValue, string currentItemName)
        {
            if (_viewModel == null || _dialog == null) return;

            // 1. 更新 ViewModel 的数据
            _viewModel.UpdateProgress(currentValue, currentItemName);

            // 2. 【关键点2】强制主线程去处理 WPF 的渲染事件，防止界面卡死白屏
            RefreshUI();
        }
        public void Stop()
        {
            if (_dialog != null)
            {
                _dialog.Close();
                _dialog = null;
                _viewModel = null;
            }
        }
        /// <summary>
        /// 强制刷新 WPF UI 的黑科技
        /// 利用 DispatcherPriority.Background 让主线程短暂歇息，去处理界面的渲染请求
        /// </summary>
        private void RefreshUI()
        {
            Dispatcher.CurrentDispatcher.Invoke(
                DispatcherPriority.Background,
                new Action(delegate { }));
        }
    }
}
