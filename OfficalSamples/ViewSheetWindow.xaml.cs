using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
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
    /// ViewSheetWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ViewSheetWindow : Window
    {
        public ViewSheetWindow()
        {
            InitializeComponent();
        }
    }
    /// <summary>
    /// 主视图模型 - 处理打印管理界面的所有业务逻辑
    /// </summary>
    public class ViewSheetViewModel : ObserverableObject, IDisposable
    {
        private readonly UIDocument _uiDocument;
        private readonly Document _document;
        private readonly ViewSheets _viewSheets;

        private ObservableCollection<ViewSheetItem> _viewSheetItems;
        private ObservableCollection<string> _viewSheetSetNames;
        private string _selectedSetName;
        private bool _showViews = true;
        private bool _showSheets = true;
        private bool _isProcessing;
        private bool _isModified;

        /// <summary>
        /// 视图/图纸项集合
        /// </summary>
        public ObservableCollection<ViewSheetItem> ViewSheetItems
        {
            get => _viewSheetItems;
            set
            {
                _viewSheetItems = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 视图/图纸集名称列表
        /// </summary>
        public ObservableCollection<string> ViewSheetSetNames
        {
            get => _viewSheetSetNames;
            set
            {
                _viewSheetSetNames = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 当前选中的视图集名称
        /// </summary>
        public string SelectedSetName
        {
            get => _selectedSetName;
            set
            {
                if (_selectedSetName != value)
                {
                    _selectedSetName = value;
                    OnPropertyChanged();
                    LoadViewSheetSet();
                }
            }
        }

        /// <summary>
        /// 是否显示视图
        /// </summary>
        public bool ShowViews
        {
            get => _showViews;
            set
            {
                if (_showViews != value)
                {
                    _showViews = value;
                    OnPropertyChanged();
                    RefreshViewSheetList();
                }
            }
        }

        /// <summary>
        /// 是否显示图纸
        /// </summary>
        public bool ShowSheets
        {
            get => _showSheets;
            set
            {
                if (_showSheets != value)
                {
                    _showSheets = value;
                    OnPropertyChanged();
                    RefreshViewSheetList();
                }
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
        /// 当前设置是否已被修改
        /// </summary>
        public bool IsModified
        {
            get => _isModified;
            set
            {
                _isModified = value;
                OnPropertyChanged();
                RefreshCommands();
            }
        }

        // 命令定义
        public ICommand OkCommand;
        public ICommand CancelCommand;
        public ICommand SaveCommand;
        public ICommand SaveAsCommand;
        public ICommand RevertCommand;
        public ICommand RenameCommand;
        public ICommand DeleteCommand;
        public ICommand CheckAllCommand;
        public ICommand CheckNoneCommand;

        public ViewSheetViewModel(UIDocument uiDocument)
        {
            _uiDocument = uiDocument ?? throw new ArgumentNullException(nameof(uiDocument));
            _document = uiDocument.Document;
            _viewSheets = new ViewSheets(_document);

            // 初始化命令
            OkCommand = new BaseBindingCommand(ExecuteOk);
            CancelCommand = new BaseBindingCommand(ExecuteCancel);
            SaveCommand = new BaseBindingCommand(ExecuteSave);
            SaveAsCommand = new BaseBindingCommand(ExecuteSaveAs);
            RevertCommand = new BaseBindingCommand(ExecuteRevert);
            RenameCommand = new BaseBindingCommand(ExecuteRename);
            DeleteCommand = new BaseBindingCommand(ExecuteDelete);
            CheckAllCommand = new BaseBindingCommand(ExecuteCheckAll);
            CheckNoneCommand = new BaseBindingCommand(ExecuteCheckNone);

            // 加载数据
            LoadData();
        }

        /// <summary>
        /// 加载初始数据
        /// </summary>
        private void LoadData()
        {
            LoadViewSheetSetNames();
            SelectedSetName = _viewSheets.SettingName;
        }

        /// <summary>
        /// 加载视图/图纸集名称列表
        /// </summary>
        private void LoadViewSheetSetNames()
        {
            var names = _viewSheets.ViewSheetSetNames;
            ViewSheetSetNames = new ObservableCollection<string>(names);
        }

        /// <summary>
        /// 加载指定的视图/图纸集
        /// </summary>
        private void LoadViewSheetSet()
        {
            if (string.IsNullOrEmpty(SelectedSetName))
                return;

            try
            {
                IsProcessing = true;
                _viewSheets.SettingName = SelectedSetName;
                RefreshViewSheetList();
                IsModified = false;
            }
            finally
            {
                IsProcessing = false;
            }
        }

        /// <summary>
        /// 刷新视图/图纸列表
        /// </summary>
        private void RefreshViewSheetList()
        {
            var visibleType = GetVisibleType();
            var items = _viewSheets.GetAvailableViewSheets(visibleType);
            ViewSheetItems = new ObservableCollection<ViewSheetItem>(items);

            // 订阅选择变化事件
            foreach (var item in ViewSheetItems)
            {
                item.PropertyChanged += OnViewSheetItemPropertyChanged;
            }
        }

        /// <summary>
        /// 根据显示选项获取可见类型
        /// </summary>
        private VisibleType GetVisibleType()
        {
            if (ShowViews && ShowSheets)
                return VisibleType.VT_BothViewAndSheet;
            if (ShowViews)
                return VisibleType.VT_ViewOnly;
            if (ShowSheets)
                return VisibleType.VT_SheetOnly;
            return VisibleType.VT_None;
        }

        /// <summary>
        /// 视图项属性变化处理
        /// </summary>
        private void OnViewSheetItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewSheetItem.IsSelected))
            {
                IsModified = true;
            }
        }

        /// <summary>
        /// 执行确定操作
        /// </summary>
        private void ExecuteOk(Object obj)
        {
            try
            {
                IsProcessing = true;
                SaveChanges();
            }
            finally
            {
                IsProcessing = false;
            }
        }

        /// <summary>
        /// 保存更改到当前视图集
        /// </summary>
        private void SaveChanges()
        {
            if (!IsModified) return;

            _viewSheets.UpdateSelectedViews(ViewSheetItems);
            IsModified = false;
        }

        /// <summary>
        /// 执行取消操作
        /// </summary>
        private void ExecuteCancel(Object obj)
        {
            CloseWindow();
        }

        /// <summary>
        /// 执行保存操作
        /// </summary>
        private void ExecuteSave(Object obj)
        {
            try
            {
                IsProcessing = true;
                SaveChanges();
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private bool CanExecuteSave() => IsModified && !IsProcessing;

        /// <summary>
        /// 执行另存为操作
        /// </summary>
        private async void ExecuteSaveAs(Object obj)
        {
            //var dialog = new System.Windows.Controls.InputDialog("另存为", "请输入新的视图集名称:");
            //if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.InputValue))
            //{
            //    try
            //    {
            //        IsProcessing = true;
            //        if (_viewSheets.SaveAs(dialog.InputValue))
            //        {
            //            LoadViewSheetSetNames();
            //            SelectedSetName = dialog.InputValue;
            //            IsModified = false;
            //        }
            //    }
            //    finally
            //    {
            //        IsProcessing = false;
            //    }
            //}
        }

        private bool CanExecuteSaveAs() => !IsProcessing;

        /// <summary>
        /// 执行恢复操作
        /// </summary>
        private void ExecuteRevert(Object obj)
        {
            try
            {
                IsProcessing = true;
                _viewSheets.Revert();
                RefreshViewSheetList();
                IsModified = false;
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private bool CanExecuteRevert() => IsModified && !IsProcessing && SelectedSetName != ConstData.InSessionName;

        /// <summary>
        /// 执行重命名操作
        /// </summary>
        private async void ExecuteRename(Object obj)
        {
            //if (SelectedSetName == ConstData.InSessionName)
            //    return;
            //var dialog = new System.Windows.Controls.InputDialog("重命名", "请输入新的名称:", SelectedSetName);
            //if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.InputValue))
            //{
            //    try
            //    {
            //        IsProcessing = true;
            //        if (_viewSheets.Rename(dialog.InputValue))
            //        {
            //            LoadViewSheetSetNames();
            //            SelectedSetName = dialog.InputValue;
            //            IsModified = false;
            //        }
            //    }
            //    finally
            //    {
            //        IsProcessing = false;
            //    }
            //}
        }

        private bool CanExecuteRename() => !IsProcessing && SelectedSetName != ConstData.InSessionName;

        /// <summary>
        /// 执行删除操作
        /// </summary>
        private void ExecuteDelete(Object obj)
        {
            if (SelectedSetName == ConstData.InSessionName)
                return;

            var result = System.Windows.MessageBox.Show(
                $"确定要删除视图集 \"{SelectedSetName}\" 吗？",
                "确认删除",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    IsProcessing = true;
                    if (_viewSheets.Delete())
                    {
                        LoadViewSheetSetNames();
                        SelectedSetName = ConstData.InSessionName;
                    }
                }
                finally
                {
                    IsProcessing = false;
                }
            }
        }

        private bool CanExecuteDelete() => !IsProcessing && SelectedSetName != ConstData.InSessionName;

        /// <summary>
        /// 全选命令执行
        /// </summary>
        private void ExecuteCheckAll(Object obj)
        {
            foreach (var item in ViewSheetItems)
            {
                item.IsSelected = true;
            }
            IsModified = true;
        }

        /// <summary>
        /// 全不选命令执行
        /// </summary>
        private void ExecuteCheckNone(Object obj)
        {
            foreach (var item in ViewSheetItems)
            {
                item.IsSelected = false;
            }
            IsModified = true;
        }

        private bool CanExecuteCheck() => ViewSheetItems?.Count > 0 && !IsProcessing;

        /// <summary>
        /// 刷新命令状态
        /// </summary>
        private void RefreshCommands()
        {
            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// 关闭窗口
        /// </summary>
        private void CloseWindow()
        {
            System.Windows.Application.Current.Windows
                .OfType<System.Windows.Window>()
                .FirstOrDefault(w => w.DataContext == this)?.Close();
        }
        public void Dispose()
        {
            ViewSheetItems?.Clear();
            ViewSheetSetNames?.Clear();
        }
    }
    /// <summary>
    /// 视图/图纸集管理类 - 封装Revit的ViewSheetSetting API
    /// </summary>
    public class ViewSheets : ISettingNameOperation
    {
        private readonly Document _document;
        private readonly ViewSheetSetting _viewSheetSetting;

        public ViewSheets(Document document)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            _viewSheetSetting = document.PrintManager.ViewSheetSetting;
        }

        /// <summary>
        /// 当前视图/图纸集名称
        /// </summary>
        public string SettingName
        {
            get
            {
                var currentSet = _viewSheetSetting.CurrentViewSheetSet;
                return currentSet is ViewSheetSet viewSheetSet ? viewSheetSet.Name : ConstData.InSessionName;
            }
            set
            {
                if (value == ConstData.InSessionName)
                {
                    _viewSheetSetting.CurrentViewSheetSet = _viewSheetSetting.InSession;
                    return;
                }

                // 使用LINQ查找指定名称的视图集
                var viewSheetSet = new FilteredElementCollector(_document)
                    .OfClass(typeof(ViewSheetSet))
                    .Cast<ViewSheetSet>()
                    .FirstOrDefault(set => set.Name == value);

                if (viewSheetSet != null)
                {
                    _viewSheetSetting.CurrentViewSheetSet = viewSheetSet;
                }
            }
        }

        public string Prefix => "Set ";

        public int SettingCount => new FilteredElementCollector(_document)
            .OfClass(typeof(ViewSheetSet))
            .ToElementIds()
            .Count;

        /// <summary>
        /// 获取所有视图/图纸集名称列表
        /// </summary>
        public List<string> ViewSheetSetNames
        {
            get
            {
                var names = new FilteredElementCollector(_document)
                    .OfClass(typeof(ViewSheetSet))
                    .Cast<ViewSheetSet>()
                    .Select(set => set.Name)
                    .ToList();

                names.Add(ConstData.InSessionName);
                return names;
            }
        }

        /// <summary>
        /// 保存当前设置为新名称
        /// </summary>
        public bool SaveAs(string newName)
        {
            try
            {
                return _viewSheetSetting.SaveAs(newName);
            }
            catch (Exception ex)
            {
                HandleException(ex);
                return false;
            }
        }

        /// <summary>
        /// 重命名当前视图集
        /// </summary>
        public bool Rename(string name)
        {
            try
            {
                return _viewSheetSetting.Rename(name);
            }
            catch (Exception ex)
            {
                HandleException(ex);
                return false;
            }
        }

        /// <summary>
        /// 保存当前视图集
        /// </summary>
        public bool Save()
        {
            try
            {
                return _viewSheetSetting.Save();
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 恢复当前视图集到上次保存的状态
        /// </summary>
        public void Revert()
        {
            try
            {
                _viewSheetSetting.Revert();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        /// <summary>
        /// 删除当前视图集
        /// </summary>
        public bool Delete()
        {
            try
            {
                return _viewSheetSetting.Delete();
            }
            catch (Exception ex)
            {
                HandleException(ex);
                return false;
            }
        }

        /// <summary>
        /// 根据可见类型获取可用的视图/图纸列表
        /// </summary>
        public List<ViewSheetItem> GetAvailableViewSheets(VisibleType visibleType)
        {
            if (visibleType == VisibleType.VT_None)
                return new List<ViewSheetItem>();

            var items = new List<ViewSheetItem>();

            foreach (View view in _viewSheetSetting.AvailableViews)
            {
                // 根据可见类型过滤
                bool isSheet = view.ViewType == ViewType.DrawingSheet;

                if (visibleType == VisibleType.VT_ViewOnly && isSheet)
                    continue;  // 仅视图模式，跳过图纸

                if (visibleType == VisibleType.VT_SheetOnly && !isSheet)
                    continue;  // 仅图纸模式，跳过视图

                var item = new ViewSheetItem
                {
                    View = view,
                    IsSelected = IsViewSelected(view)
                };
                items.Add(item);
            }

            return items;
        }

        /// <summary>
        /// 检查指定视图是否在当前视图中被选中
        /// </summary>
        private bool IsViewSelected(View view)
        {
            var viewKey = $"{view.ViewType}: {view.Name}";
            return _viewSheetSetting.CurrentViewSheetSet.Views
                .Cast<View>()
                .Any(v => $"{v.ViewType}: {v.Name}" == viewKey);
        }

        /// <summary>
        /// 更新当前视图集的选中项
        /// </summary>
        public void UpdateSelectedViews(IEnumerable<ViewSheetItem> selectedItems)
        {
            var selectedViews = new ViewSet();

            foreach (var item in selectedItems.Where(item => item.IsSelected))
            {
                selectedViews.Insert(item.View);
            }

            var currentSet = _viewSheetSetting.CurrentViewSheetSet;
            currentSet.Views = selectedViews;
            Save();
        }

        /// <summary>
        /// 统一异常处理
        /// </summary>
        private static void HandleException(Exception ex)
        {
            // 在实际应用中，这里应该使用日志记录或消息提示
            System.Diagnostics.Debug.WriteLine($"操作失败: {ex.Message}");
        }
    }
    /// <summary>
    /// 视图/图纸项数据模型 - 表示单个视图或图纸
    /// </summary>
    public class ViewSheetItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        private View _view;
        private string _displayName;

        /// <summary>
        /// 原始Revit视图对象
        /// </summary>
        public View View
        {
            get => _view;
            set
            {
                _view = value;
                OnPropertyChanged();
                UpdateDisplayName();
            }
        }

        /// <summary>
        /// 显示名称（格式：视图类型: 视图名称）
        /// </summary>
        public string DisplayName
        {
            get => _displayName;
            set
            {
                _displayName = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 是否被选中（用于打印）
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 视图类型（用于UI显示）
        /// </summary>
        public string ViewTypeName => _view?.ViewType.ToString() ?? "Unknown";

        /// <summary>
        /// 更新显示名称
        /// </summary>
        private void UpdateDisplayName()
        {
            if (_view != null)
            {
                DisplayName = $"{_view.ViewType}: {_view.Name}";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    /// <summary>
    /// 常量数据定义类 - 存放应用程序中使用的常量
    /// </summary>
    public static class ConstData
    {
        /// <summary>
        /// 会话内打印设置和视图/图纸集的名称
        /// 表示未保存到文档中的临时设置
        /// </summary>
        public const string InSessionName = "<In-Session>";
    }
    /// <summary>
    /// 可见类型枚举 - 控制显示视图还是图纸
    /// </summary>
    public enum VisibleType
    {
        VT_ViewOnly,        // 仅显示视图
        VT_SheetOnly,       // 仅显示图纸
        VT_BothViewAndSheet, // 同时显示视图和图纸
        VT_None             // 不显示
    }

    /// <summary>
    /// 设置名称操作接口 - 定义视图/图纸集的基本操作
    /// </summary>
    public interface ISettingNameOperation
    {
        string SettingName { get; set; }
        string Prefix { get; }
        int SettingCount { get; }
        bool Rename(string name);
        bool SaveAs(string newName);
    }

    /// <summary>
    /// 数量转可见性转换器
    /// </summary>
    public class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                return count > 0 ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
            }
            return System.Windows.Visibility.Visible;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
