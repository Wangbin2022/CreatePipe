using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
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
using Path = System.IO.Path;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// DragAndDropWindow.xaml 的交互逻辑
    /// </summary>
    public partial class DragAndDropWindow : Window
    {
        private System.Windows.Point _dragStartPoint;
        private FurnitureFamilyItem _draggedItem;
        public DragAndDropWindow(DragAndDropViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            Closed += (s, e) => viewModel.Dispose();
        }
        /// <summary>
        /// 鼠标按下事件 - 记录拖拽开始点
        /// </summary>
        private void ListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);

            var listBox = sender as System.Windows.Controls.ListBox;
            _draggedItem = (listBox?.SelectedItem as FurnitureFamilyItem);
        }

        /// <summary>
        /// 鼠标移动事件 - 判断是否开始拖拽
        /// </summary>
        private void ListBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            var currentPoint = e.GetPosition(null);
            var vector = _dragStartPoint - currentPoint;

            // 如果移动距离足够，开始拖拽操作
            if (Math.Abs(vector.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(vector.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                if (_draggedItem != null)
                {
                    var data = new DataObject(typeof(FurnitureFamilyItem), _draggedItem);
                    DragDrop.DoDragDrop(this, data, DragDropEffects.Move);
                }
            }
        }

        /// <summary>
        /// 拖拽经过事件 - 设置拖拽效果
        /// </summary>
        private void ListBox_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(FurnitureFamilyItem)))
            {
                var draggedItem = e.Data.GetData(typeof(FurnitureFamilyItem)) as FurnitureFamilyItem;
                var targetListBox = sender as System.Windows.Controls.ListBox;

                if (draggedItem != null && targetListBox != null)
                {
                    // 判断拖拽方向是否合理
                    var isSourceLoaded = draggedItem.IsLoaded;
                    var isTargetLoaded = targetListBox.Name == "LoadedListBox";

                    // 只允许在不同类型列表间拖拽
                    if (isSourceLoaded != isTargetLoaded)
                    {
                        e.Effects = DragDropEffects.Move;
                    }
                    else
                    {
                        e.Effects = DragDropEffects.None;
                    }
                }
            }

            e.Handled = true;
        }

        /// <summary>
        /// 拖拽放置事件 - 执行加载或卸载操作
        /// </summary>
        private async void ListBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(FurnitureFamilyItem)))
            {
                var draggedItem = e.Data.GetData(typeof(FurnitureFamilyItem)) as FurnitureFamilyItem;
                var targetListBox = sender as System.Windows.Controls.ListBox;
                var viewModel = DataContext as DragAndDropViewModel;

                if (draggedItem != null && targetListBox != null && viewModel != null)
                {
                    // 判断目标列表类型并执行相应操作
                    var isTargetLoaded = targetListBox.Name == "LoadedListBox";

                    if (draggedItem.IsLoaded && !isTargetLoaded)
                    {
                        // 从已加载拖到可用 - 执行卸载
                        viewModel.SelectedLoadedFamily = draggedItem;
                        viewModel.UnloadFamilyCommand.Execute(null);
                    }
                    else if (!draggedItem.IsLoaded && isTargetLoaded)
                    {
                        // 从可用拖到已加载 - 执行加载
                        viewModel.SelectedAvailableFamily = draggedItem;
                        viewModel.LoadFamilyCommand.Execute(null);
                    }
                }
            }

            e.Handled = true;
        }
    }
    /// <summary>
    /// 主视图模型 - 处理家具族拖放功能的所有业务逻辑
    /// </summary>
    public class DragAndDropViewModel : ObserverableObject, IDisposable
    {
        private readonly RevitFamilyService _familyService;
        private ObservableCollection<FurnitureFamilyItem> _loadedFamilies;
        private ObservableCollection<FurnitureFamilyItem> _availableFamilies;
        private FurnitureFamilyItem _selectedLoadedFamily;
        private FurnitureFamilyItem _selectedAvailableFamily;
        private bool _isProcessing;
        private string _currentFolderPath;

        /// <summary>
        /// 已加载的家具族集合
        /// </summary>
        public ObservableCollection<FurnitureFamilyItem> LoadedFamilies
        {
            get => _loadedFamilies;
            set
            {
                _loadedFamilies = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 可用的家具族文件集合
        /// </summary>
        public ObservableCollection<FurnitureFamilyItem> AvailableFamilies
        {
            get => _availableFamilies;
            set
            {
                _availableFamilies = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 当前选中的已加载族
        /// </summary>
        public FurnitureFamilyItem SelectedLoadedFamily
        {
            get => _selectedLoadedFamily;
            set
            {
                _selectedLoadedFamily = value;
                OnPropertyChanged();
                ((BaseBindingCommand)UnloadFamilyCommand)?.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// 当前选中的可用族
        /// </summary>
        public FurnitureFamilyItem SelectedAvailableFamily
        {
            get => _selectedAvailableFamily;
            set
            {
                _selectedAvailableFamily = value;
                OnPropertyChanged();
                ((BaseBindingCommand)LoadFamilyCommand)?.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// 是否正在处理中
        /// </summary>
        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                _isProcessing = value;
                OnPropertyChanged();
                RefreshCommands();
            }
        }

        /// <summary>
        /// 当前族库文件夹路径
        /// </summary>
        public string CurrentFolderPath
        {
            get => _currentFolderPath;
            set
            {
                _currentFolderPath = value;
                OnPropertyChanged();
                RefreshAvailableFamilies();
            }
        }

        // 命令定义
        public ICommand LoadFamilyCommand;
        public ICommand UnloadFamilyCommand;
        public ICommand RefreshCommand;
        public ICommand BrowseFolderCommand;
        public ICommand LoadToDragTargetCommand;
        public ICommand UnloadToDragTargetCommand;

        public DragAndDropViewModel(UIDocument uiDocument)
        {
            _familyService = new RevitFamilyService(uiDocument);

            // 初始化命令
            LoadFamilyCommand = new BaseBindingCommand(ExecuteLoadFamily);
            UnloadFamilyCommand = new BaseBindingCommand(ExecuteUnloadFamily);
            RefreshCommand = new BaseBindingCommand(ExecuteRefresh);
            BrowseFolderCommand = new BaseBindingCommand(ExecuteBrowseFolder);
            LoadToDragTargetCommand = new BaseBindingCommand(ExecuteLoadToTarget);
            UnloadToDragTargetCommand = new BaseBindingCommand(ExecuteUnloadToTarget);

            // 初始化数据
            InitializeData();
        }

        /// <summary>
        /// 初始化数据
        /// </summary>
        private async void InitializeData()
        {
            try
            {
                IsProcessing = true;

                // 异步加载数据
                var loaded = await System.Threading.Tasks.Task.Run(() => _familyService.GetLoadedFurnitureFamilies());
                LoadedFamilies = new ObservableCollection<FurnitureFamilyItem>(loaded);

                // 设置默认路径
                CurrentFolderPath = RevitFamilyService.GetDefaultFamilyFolder();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        /// <summary>
        /// 刷新可用族列表
        /// </summary>
        private async void RefreshAvailableFamilies()
        {
            if (string.IsNullOrWhiteSpace(CurrentFolderPath))
                return;

            try
            {
                IsProcessing = true;

                var families = await System.Threading.Tasks.Task.Run(() =>
                    _familyService.GetAvailableFamilyFiles(CurrentFolderPath));

                AvailableFamilies = new ObservableCollection<FurnitureFamilyItem>(families);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"刷新列表失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        /// <summary>
        /// 执行加载族命令
        /// </summary>
        private async void ExecuteLoadFamily(Object obj)
        {
            if (SelectedAvailableFamily == null)
                return;

            try
            {
                IsProcessing = true;

                // 异步加载族
                var success = await System.Threading.Tasks.Task.Run(() =>
                    _familyService.LoadFamily(SelectedAvailableFamily));

                if (success)
                {
                    // 从可用列表移除，添加到已加载列表
                    AvailableFamilies.Remove(SelectedAvailableFamily);
                    LoadedFamilies.Add(SelectedAvailableFamily);
                    SelectedAvailableFamily = null;

                    MessageBox.Show("族加载成功！", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("族加载失败！", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private bool CanExecuteLoadFamily()
        {
            return SelectedAvailableFamily != null && !IsProcessing;
        }

        /// <summary>
        /// 执行卸载族命令
        /// </summary>
        private async void ExecuteUnloadFamily(Object obj)
        {
            if (SelectedLoadedFamily == null)
                return;

            // 检查是否可以卸载
            if (!_familyService.CanUnloadFamily(SelectedLoadedFamily.Name))
            {
                var result = MessageBox.Show(
                    "该族在项目中有实例存在，卸载可能会导致数据丢失。确定要继续吗？",
                    "警告",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;
            }

            try
            {
                IsProcessing = true;

                var success = await System.Threading.Tasks.Task.Run(() =>
                    _familyService.UnloadFamily(SelectedLoadedFamily));

                if (success)
                {
                    // 从已加载列表移除
                    LoadedFamilies.Remove(SelectedLoadedFamily);
                    SelectedLoadedFamily = null;

                    MessageBox.Show("族卸载成功！", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("族卸载失败！", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"卸载失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsProcessing = false;
                ExecuteRefresh(null); // 刷新列表
            }
        }

        private bool CanExecuteUnloadFamily()
        {
            return SelectedLoadedFamily != null && !IsProcessing;
        }

        /// <summary>
        /// 执行刷新命令（重新加载数据）
        /// </summary>
        private void ExecuteRefresh(Object obj)
        {
            InitializeData();
            RefreshAvailableFamilies();
        }

        /// <summary>
        /// 浏览文件夹命令
        /// </summary>
        private void ExecuteBrowseFolder(Object obj)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "选择家具族库文件夹";
                dialog.SelectedPath = CurrentFolderPath;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    CurrentFolderPath = dialog.SelectedPath;
                }
            }
        }

        /// <summary>
        /// 拖放加载到目标列表（从可用到已加载）
        /// </summary>
        private void ExecuteLoadToTarget(Object obj)
        {
            // 这个方法由拖放操作触发
            if (SelectedAvailableFamily != null)
            {
                ExecuteLoadFamily(null);
            }
        }

        /// <summary>
        /// 拖放卸载到目标列表（从已加载到可用）
        /// </summary>
        private void ExecuteUnloadToTarget(Object obj)
        {
            if (SelectedLoadedFamily != null)
            {
                ExecuteUnloadFamily(null);
            }
        }
        /// <summary>
        /// 刷新命令状态
        /// </summary>
        private void RefreshCommands()
        {
            CommandManager.InvalidateRequerySuggested();
        }
        public void Dispose()
        {
            _familyService?.Dispose();
            LoadedFamilies?.Clear();
            AvailableFamilies?.Clear();
        }
    }
    /// <summary>
    /// Revit族管理服务类
    /// 负责加载、卸载和查询家具族
    /// </summary>
    public class RevitFamilyService : IDisposable
    {
        private readonly UIDocument _uiDocument;
        private readonly Document _document;
        private bool _disposed;

        public RevitFamilyService(UIDocument uiDocument)
        {
            _uiDocument = uiDocument ?? throw new ArgumentNullException(nameof(uiDocument));
            _document = uiDocument.Document;
        }

        /// <summary>
        /// 获取当前文档中所有已加载的家具族
        /// </summary>
        public List<FurnitureFamilyItem> GetLoadedFurnitureFamilies()
        {
            var families = new FilteredElementCollector(_document)
                .OfClass(typeof(Family))
                .Cast<Family>()
                .Where(family => family.FamilyCategory != null &&
                                family.FamilyCategory.Name.Contains("家具") ||
                                family.FamilyCategory.Name.Contains("Furniture"))
                .Select(family => new FurnitureFamilyItem
                {
                    Name = family.Name,
                    IsLoaded = true,
                    Path = string.Empty
                })
                .OrderBy(f => f.Name)
                .ToList();

            return families;
        }

        /// <summary>
        /// 获取指定文件夹中的家具族文件列表
        /// </summary>
        /// <param name="folderPath">文件夹路径</param>
        public List<FurnitureFamilyItem> GetAvailableFamilyFiles(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
                return new List<FurnitureFamilyItem>();

            var familyFiles = Directory.GetFiles(folderPath, "*.rfa")
                .Where(file => file.Contains("家具") || file.Contains("Furniture"))
                .Select(file => new FurnitureFamilyItem
                {
                    Name = Path.GetFileNameWithoutExtension(file),
                    Path = file,
                    IsLoaded = false
                })
                .OrderBy(f => f.Name)
                .ToList();

            return familyFiles;
        }

        /// <summary>
        /// 加载家具族到文档
        /// </summary>
        /// <param name="familyItem">要加载的族项目</param>
        /// <returns>是否成功</returns>
        public bool LoadFamily(FurnitureFamilyItem familyItem)
        {
            if (familyItem.IsLoaded || string.IsNullOrWhiteSpace(familyItem.Path))
                return false;

            try
            {
                using (var transaction = new Transaction(_document, "加载家具族"))
                {
                    transaction.Start();

                    // 尝试加载族文件
                    if (_document.LoadFamily(familyItem.Path, out Family family))
                    {
                        familyItem.IsLoaded = true;
                        familyItem.Status = "已加载成功";
                        transaction.Commit();
                        return true;
                    }

                    transaction.RollBack();
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载族失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 卸载文档中的家具族
        /// </summary>
        /// <param name="familyItem">要卸载的族</param>
        /// <returns>是否成功</returns>
        public bool UnloadFamily(FurnitureFamilyItem familyItem)
        {
            if (!familyItem.IsLoaded)
                return false;

            try
            {
                // 查找要卸载的族
                var family = new FilteredElementCollector(_document)
                    .OfClass(typeof(Family))
                    .Cast<Family>()
                    .FirstOrDefault(f => f.Name == familyItem.Name);

                if (family == null)
                    return false;

                using (var transaction = new Transaction(_document, "卸载家具族"))
                {
                    transaction.Start();

                    // 卸载族（如果有实例存在，会失败）
                    _document.Delete(family.Id);
                    familyItem.IsLoaded = false;
                    familyItem.Status = "已卸载";

                    transaction.Commit();
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"卸载族失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 检查族是否可以被卸载（没有实例在使用）
        /// </summary>
        public bool CanUnloadFamily(string familyName)
        {
            var familyInstances = new FilteredElementCollector(_document)
                .OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>()
                .Any(instance => instance.Symbol.Family.Name == familyName);

            return !familyInstances;
        }

        /// <summary>
        /// 获取所有可用的家具族文件夹路径
        /// </summary>
        public static string GetDefaultFamilyFolder()
        {
            // 获取Revit默认族库路径
            var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var defaultPath = Path.Combine(programDataPath, "Autodesk", "RVT", "Libraries");

            // 如果默认路径不存在，返回当前目录
            return Directory.Exists(defaultPath) ? defaultPath : Directory.GetCurrentDirectory();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // 清理资源
                _disposed = true;
            }
        }
    }
    /// <summary>
    /// 家具族项目数据模型
    /// 表示单个家具族文件或已加载的族实例
    /// </summary>
    public class FurnitureFamilyItem : ObserverableObject
    {
        private string _name;
        private string _path;
        private bool _isLoaded;
        private string _status;

        /// <summary>
        /// 族名称
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 文件路径（仅用于未加载的族）
        /// </summary>
        public string Path
        {
            get => _path;
            set
            {
                _path = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 是否已加载到文档中
        /// </summary>
        public bool IsLoaded
        {
            get => _isLoaded;
            set
            {
                _isLoaded = value;
                OnPropertyChanged();
                UpdateStatus();
            }
        }

        /// <summary>
        /// 状态描述
        /// </summary>
        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 显示名称（用于UI）
        /// </summary>
        public string DisplayName => $"{Name} ({(IsLoaded ? "已加载" : "未加载")})";

        /// <summary>
        /// 更新状态信息
        /// </summary>
        private void UpdateStatus()
        {
            Status = IsLoaded ? "已加载到项目" : "待加载";
        }
    }

    /// <summary>
    /// 字符串到可见性转换器
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrWhiteSpace(value as string) ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    /// <summary>
    /// 状态颜色转换器
    /// </summary>
    public class StatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isLoaded)
            {
                return isLoaded ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80)) : new SolidColorBrush(System.Windows.Media.Color.FromRgb(158, 158, 158));
            }
            return new SolidColorBrush(Colors.Gray);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    /// <summary>
    /// 状态文本转换器
    /// </summary>
    public class StatusTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isLoaded)
            {
                return isLoaded ? "已加载" : "未加载";
            }
            return "未知";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
