using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// TypeSelectorView.xaml 的交互逻辑
    /// </summary>
    public partial class TypeSelectorView : Window
    {
        public TypeSelectorView(TypeSelectorViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            Closed += (s, e) => viewModel.Dispose();
        }
        /// <summary>
        /// 搜索框文本变化事件 - 过滤类型列表
        /// </summary>
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var viewModel = DataContext as TypeSelectorViewModel;
            var searchText = (sender as System.Windows.Controls.TextBox)?.Text?.ToLower();

            if (viewModel?.AvailableTypes != null && !string.IsNullOrWhiteSpace(searchText))
            {
                // 注意：这里需要实现过滤逻辑
                // 可以在ViewModel中添加过滤属性
            }
        }

        /// <summary>
        /// 取消按钮点击事件
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as TypeSelectorViewModel;
            viewModel?.CancelCommand.Execute(null);
        }
        /// <summary>
        /// 支持键盘快捷键
        /// </summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.Escape)
            {
                CancelButton_Click(this, new RoutedEventArgs());
            }
            else if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                var viewModel = DataContext as TypeSelectorViewModel;
                if (viewModel?.OkCommand.CanExecute(null) == true)
                {
                    viewModel.OkCommand.Execute(null);
                }
            }
        }
    }
    /// <summary>
    /// 主视图模型 - 处理类型选择器的所有业务逻辑
    /// </summary>
    public class TypeSelectorViewModel : ObserverableObject, IDisposable
    {
        private readonly RevitTypeService _typeService;
        private ObservableCollection<TypeItem> _availableTypes;
        private TypeItem _selectedType;
        private Element _currentElement;
        private string _currentElementInfo;
        private bool _isLoading;
        private string _statusMessage;
        private bool _isDisposed;

        /// <summary>
        /// 可用的类型列表
        /// </summary>
        public ObservableCollection<TypeItem> AvailableTypes
        {
            get => _availableTypes;
            set
            {
                _availableTypes = value;
                OnPropertyChanged();
                UpdateStatusMessage();
            }
        }

        /// <summary>
        /// 当前选中的类型
        /// </summary>
        public TypeItem SelectedType
        {
            get => _selectedType;
            set
            {
                _selectedType = value;
                OnPropertyChanged();
                ((BaseBindingCommand)OkCommand)?.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// 当前选中元素的信息
        /// </summary>
        public string CurrentElementInfo
        {
            get => _currentElementInfo;
            set
            {
                _currentElementInfo = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 是否正在加载数据
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        /// <summary>
        /// 状态消息
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 类型总数
        /// </summary>
        public int TotalTypeCount => AvailableTypes?.Count ?? 0;

        /// <summary>
        /// 当前类型索引显示
        /// </summary>
        public string TypeIndexDisplay => SelectedType != null ?
            $"位置: {AvailableTypes.IndexOf(SelectedType) + 1}/{TotalTypeCount}" :
            "未选择类型";

        // 命令定义
        public ICommand OkCommand;
        public ICommand CancelCommand;
        public ICommand RefreshCommand;

        public TypeSelectorViewModel(UIDocument uiDocument)
        {
            _typeService = new RevitTypeService(uiDocument);

            // 初始化命令
            OkCommand = new BaseBindingCommand(ExecuteOk);
            CancelCommand = new BaseBindingCommand(ExecuteCancel);
            RefreshCommand = new BaseBindingCommand(ExecuteRefresh);

            // 加载数据
            LoadElementAndTypes();
        }

        /// <summary>
        /// 加载选中的元素和可用类型
        /// </summary>
        private async void LoadElementAndTypes()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在加载元素信息...";

                // 异步加载元素和类型
                var element = await System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        return _typeService.GetSelectedElement();
                    }
                    catch (InvalidOperationException ex)
                    {
                        throw new InvalidOperationException(ex.Message);
                    }
                });

                _currentElement = element;

                var elementType = element is Wall ? "墙体" : "构件";
                var typeName = _typeService.GetCurrentTypeName(element);
                CurrentElementInfo = $"当前选中: {elementType} - {typeName} (ID: {element.Id.IntegerValue})";

                StatusMessage = "正在加载可用类型...";

                var types = await System.Threading.Tasks.Task.Run(() =>
                    _typeService.GetAvailableTypes(element));

                AvailableTypes = new ObservableCollection<TypeItem>(types);

                // 自动选中当前类型
                SelectedType = AvailableTypes.FirstOrDefault(t => t.IsSelected);

                StatusMessage = $"已加载 {TotalTypeCount} 个可用类型";
            }
            catch (InvalidOperationException ex)
            {
                StatusMessage = ex.Message;
                var result = MessageBox.Show($"{ex.Message}\n是否重新选择？", "提示",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    ExecuteRefresh(null);
                }
                else
                {
                    ExecuteCancel(null);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"加载失败: {ex.Message}";
                MessageBox.Show($"加载失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                ExecuteCancel(null);
            }
            finally
            {
                IsLoading = false;
                OnPropertyChanged(nameof(TotalTypeCount));
            }
        }

        /// <summary>
        /// 执行确定命令 - 更改元素类型
        /// </summary>
        private async void ExecuteOk(Object obj)
        {
            if (SelectedType == null || _currentElement == null)
                return;

            try
            {
                IsLoading = true;
                StatusMessage = "正在更改类型...";

                var success = await System.Threading.Tasks.Task.Run(() =>
                    _typeService.ChangeElementType(_currentElement, SelectedType));

                if (success)
                {
                    StatusMessage = $"类型已成功更改为: {SelectedType.Name}";
                    MessageBox.Show($"元素类型已成功更改为 {SelectedType.Name}", "成功",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    ExecuteCancel(null);
                }
                else
                {
                    StatusMessage = "类型更改失败";
                    MessageBox.Show("类型更改失败，请确保没有其他限制", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"更改失败: {ex.Message}";
                MessageBox.Show($"更改失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanExecuteOk()
        {
            return SelectedType != null && !IsLoading && _currentElement != null;
        }

        /// <summary>
        /// 执行取消命令 - 关闭窗口
        /// </summary>
        private void ExecuteCancel(Object obj)
        {
            CloseWindow();
        }

        /// <summary>
        /// 执行刷新命令 - 重新加载
        /// </summary>
        private void ExecuteRefresh(Object obj)
        {
            // 清除当前状态并重新加载
            AvailableTypes?.Clear();
            SelectedType = null;
            LoadElementAndTypes();
        }

        /// <summary>
        /// 更新状态消息
        /// </summary>
        private void UpdateStatusMessage()
        {
            if (AvailableTypes != null && !string.IsNullOrEmpty(StatusMessage) && !IsLoading)
            {
                StatusMessage = $"已加载 {TotalTypeCount} 个可用类型";
                OnPropertyChanged(nameof(TypeIndexDisplay));
            }
        }

        /// <summary>
        /// 关闭窗口
        /// </summary>
        private void CloseWindow()
        {
            var window = System.Windows.Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w.DataContext == this);
            window?.Close();
        }
        public void Dispose()
        {
            if (!_isDisposed)
            {
                _typeService?.Dispose();
                AvailableTypes?.Clear();
                _isDisposed = true;
            }
        }
    }
    /// <summary>
    /// Revit类型服务类
    /// 负责获取和修改元素类型
    /// </summary>
    public class RevitTypeService : IDisposable
    {
        private readonly UIDocument _uiDocument;
        private readonly Document _document;
        private bool _disposed;

        public RevitTypeService(UIDocument uiDocument)
        {
            _uiDocument = uiDocument ?? throw new ArgumentNullException(nameof(uiDocument));
            _document = uiDocument.Document;
        }

        /// <summary>
        /// 获取用户选中的元素（仅支持墙体或构件）
        /// </summary>
        public Element GetSelectedElement()
        {
            var selection = _uiDocument.Selection;
            var elementIds = selection.GetElementIds();

            if (elementIds.Count != 1)
                throw new InvalidOperationException("请选择一个墙体或构件");

            var element = _document.GetElement(elementIds.First());

            if (!(element is Wall || element is FamilyInstance))
                throw new InvalidOperationException("选中的元素必须是墙体或构件");

            return element;
        }

        /// <summary>
        /// 获取元素可用的所有类型
        /// </summary>
        public List<TypeItem> GetAvailableTypes(Element element)
        {
            if (element is Wall)
            {
                var wall = (Wall)element;
                return GetWallTypes(wall);
            }
            else if (element is FamilyInstance)
            {
                var instance = (FamilyInstance)element;
                return GetFamilyTypes(instance);
            }
            else
            {
                throw new ArgumentException("不支持的元件类型");
            }
        }

        /// <summary>
        /// 获取墙体可用的所有墙体类型
        /// </summary>
        private List<TypeItem> GetWallTypes(Wall wall)
        {
            var currentTypeId = wall.WallType?.Id.IntegerValue ?? 0;

            var wallTypes = new FilteredElementCollector(_document)
                .OfClass(typeof(WallType))
                .Cast<WallType>()
                .Select(type => new TypeItem
                {
                    Name = type.Name,
                    Id = type.Id.IntegerValue,
                    IsSelected = type.Id.IntegerValue == currentTypeId
                })
                .OrderBy(t => t.Name)
                .ToList();

            return wallTypes;
        }

        /// <summary>
        /// 获取构件可用的所有族类型
        /// </summary>
        private List<TypeItem> GetFamilyTypes(FamilyInstance instance)
        {
            var currentTypeId = instance.Symbol?.Id.IntegerValue ?? 0;
            var family = instance.Symbol.Family;

            var familyTypes = family.GetFamilySymbolIds()
                .Select(id => _document.GetElement(id) as FamilySymbol)
                .Where(symbol => symbol != null)
                .Select(symbol => new TypeItem
                {
                    Name = symbol.Name,
                    Id = symbol.Id.IntegerValue,
                    IsSelected = symbol.Id.IntegerValue == currentTypeId
                })
                .OrderBy(t => t.Name)
                .ToList();

            return familyTypes;
        }

        /// <summary>
        /// 更改元素的类型
        /// </summary>
        public bool ChangeElementType(Element element, TypeItem targetType)
        {
            try
            {
                using (var transaction = new Transaction(_document, "更改元素类型"))
                {
                    transaction.Start();

                    var elementId = new ElementId(targetType.Id);
                    var success = false;

                    switch (element)
                    {
                        case Wall wall:
                            var wallType = _document.GetElement(elementId) as WallType;
                            if (wallType != null)
                            {
                                wall.WallType = wallType;
                                success = true;
                            }
                            break;

                        case FamilyInstance instance:
                            var familySymbol = _document.GetElement(elementId) as FamilySymbol;
                            if (familySymbol != null)
                            {
                                instance.Symbol = familySymbol;
                                success = true;
                            }
                            break;
                    }

                    if (success)
                        transaction.Commit();
                    else
                        transaction.RollBack();

                    return success;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更改类型失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取选中元素的当前类型名称
        /// </summary>
        public string GetCurrentTypeName(Element element)
        {
            if (element is Wall)
            {
                var wall = (Wall)element;
                if (wall.WallType != null)
                {
                    return wall.WallType.Name;
                }
                return "未知";
            }
            else if (element is FamilyInstance)
            {
                var instance = (FamilyInstance)element;
                if (instance.Symbol != null)
                {
                    return instance.Symbol.Name;
                }
                return "未知";
            }
            else
            {
                return "未知";
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }
    /// <summary>
    /// 类型项数据模型
    /// 表示Revit中的类型（墙体类型或构件类型）
    /// </summary>
    public class TypeItem : ObserverableObject
    {
        private string _name;
        private int _id;
        private bool _isSelected;
        private string _description;

        /// <summary>
        /// 类型名称
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
                UpdateDescription();
            }
        }

        /// <summary>
        /// 类型ID（ElementId的整数值）
        /// </summary>
        public int Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 是否被选中
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
        /// 类型描述（显示在UI上）
        /// </summary>
        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 更新描述信息
        /// </summary>
        private void UpdateDescription()
        {
            Description = $"{Name} (ID: {Id})";
        }
    }
    /// <summary>
    /// 类型项模板选择器
    /// </summary>
    public class TypeItemTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var element = container as FrameworkElement;
            return element?.FindResource("TypeItemTemplate") as DataTemplate;
        }
    }
}
