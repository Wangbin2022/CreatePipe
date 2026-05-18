using CreatePipe.cmd;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Effects;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// TagRebarView.xaml 的交互逻辑
    /// </summary>
    public partial class TagRebarView : Window
    {
        private readonly TagRebarViewModel _viewModel;
        public TagRebarView(TagRebarViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
            _viewModel = viewModel;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
    /// <summary>
    /// 下拉阴影效果
    /// </summary>
    public static class EffectResources
    {
        public static DropShadowEffect DropShadowEffect => new DropShadowEffect
        {
            ShadowDepth = 2,
            BlurRadius = 8,
            Opacity = 0.3
        };
    }
    /// <summary>
    /// 钢筋标记视图模型
    /// </summary>
    public class TagRebarViewModel : ObserverableObject
    {
        private readonly TaggingService _service;
        private ObservableCollection<RebarInfo> _selectedRebars;
        private string _statusMessage;
        private bool _isProcessing;

        public ObservableCollection<RebarInfo> SelectedRebars
        {
            get => _selectedRebars;
            set { _selectedRebars = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set { _isProcessing = value; OnPropertyChanged(); }
        }

        public ICommand CreateTagCommand { get; }
        public ICommand CreateTextCommand { get; }
        public ICommand RefreshCommand { get; }

        public TagRebarViewModel(TaggingService service)
        {
            _service = service;
            SelectedRebars = new ObservableCollection<RebarInfo>();

            CreateTagCommand = new BaseBindingCommand(ExecuteCreateTag, _ => SelectedRebars.Any() && !IsProcessing);
            CreateTextCommand = new BaseBindingCommand(ExecuteCreateText, _ => SelectedRebars.Any() && !IsProcessing);
            RefreshCommand = new BaseBindingCommand(ExecuteRefresh);

            LoadData();
        }

        private async void ExecuteCreateTag(Object obj)
        {
            IsProcessing = true;
            StatusMessage = "正在创建钢筋标记...";

            await System.Threading.Tasks.Task.Run(() =>
            {
                int successCount = 0;
                foreach (var rebarInfo in SelectedRebars)
                {
                    if (_service.CreateRebarTag(rebarInfo.Rebar))
                        successCount++;
                }

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusMessage = $"成功创建了 {successCount} 个钢筋标记（共 {SelectedRebars.Count} 个）";
                });
            });

            IsProcessing = false;
        }

        private async void ExecuteCreateText(Object obj)
        {
            IsProcessing = true;
            StatusMessage = "正在创建文字注释...";

            await System.Threading.Tasks.Task.Run(() =>
            {
                int successCount = 0;
                foreach (var rebarInfo in SelectedRebars)
                {
                    if (_service.CreateRebarTextNote(rebarInfo.Rebar))
                        successCount++;
                }

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusMessage = $"成功创建了 {successCount} 个文字注释（共 {SelectedRebars.Count} 个）";
                });
            });

            IsProcessing = false;
        }

        private async void ExecuteRefresh(Object obj)
        {
            IsProcessing = true;
            StatusMessage = "正在刷新数据...";

            await System.Threading.Tasks.Task.Run(() => LoadData());

            IsProcessing = false;
            StatusMessage = $"数据已刷新，找到 {SelectedRebars.Count} 个钢筋";
        }

        private void LoadData()
        {
            var rebars = _service.GetSelectedRebars();
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                SelectedRebars.Clear();
                foreach (var rebar in rebars)
                    SelectedRebars.Add(rebar);
            });
        }
    }
}
