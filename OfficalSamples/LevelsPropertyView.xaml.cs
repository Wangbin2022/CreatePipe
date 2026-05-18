using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// LevelsPropertyView.xaml 的交互逻辑
    /// </summary>
    public partial class LevelsPropertyView : Window
    {
        private readonly LevelsPropertyViewModel _viewModel;
        public LevelsPropertyView(UIApplication uiApp)
        {
            InitializeComponent();
            _viewModel = new LevelsPropertyViewModel(uiApp);
            DataContext = _viewModel;
        }
        protected override void OnClosed(EventArgs e)
        {
            _viewModel.Dispose();
            base.OnClosed(e);
        }
    }
    /// <summary>
    /// 标高管理主视图模型
    /// </summary>
    public class LevelsPropertyViewModel : ObserverableObject, IDisposable
    {
        private readonly UIApplication _uiApp;
        private readonly LevelService2 _levelService;
        private Transaction _transaction;

        #region 属性

        private ObservableCollection<LevelItem> _levels;
        public ObservableCollection<LevelItem> Levels
        {
            get => _levels;
            set => SetProperty(ref _levels, value);
        }

        private LevelItem _selectedLevel;
        public LevelItem SelectedLevel
        {
            get => _selectedLevel;
            set => SetProperty(ref _selectedLevel, value);
        }

        private string _newLevelName;
        public string NewLevelName
        {
            get => _newLevelName;
            set => SetProperty(ref _newLevelName, value);
        }

        private double _newLevelElevation;
        public double NewLevelElevation
        {
            get => _newLevelElevation;
            set => SetProperty(ref _newLevelElevation, value);
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        private string _unitDisplay;
        public string UnitDisplay
        {
            get => _unitDisplay;
            set => SetProperty(ref _unitDisplay, value);
        }

        private bool _hasChanges;
        public bool HasChanges
        {
            get => _hasChanges;
            set => SetProperty(ref _hasChanges, value);
        }

        #endregion

        #region 命令

        public ICommand SaveCommand { get; }
        public ICommand AddLevelCommand { get; }
        public ICommand DeleteLevelCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand CloseCommand { get; }

        #endregion

        public LevelsPropertyViewModel(UIApplication uiApp)
        {
            _uiApp = uiApp;
            _levelService = new LevelService2(uiApp);

            // 初始化命令
            SaveCommand = new BaseBindingCommand(_ => SaveChanges(), _ => HasChanges && !IsBusy);
            AddLevelCommand = new BaseBindingCommand(_ => AddNewLevel(), _ => CanAddLevel());
            DeleteLevelCommand = new BaseBindingCommand(_ => DeleteLevel(), _ => SelectedLevel != null && !IsBusy);
            RefreshCommand = new BaseBindingCommand(_ => LoadData());
            CloseCommand = new BaseBindingCommand(_ => CloseWindow());
            // 加载数据
            LoadData();
            //// 设置单位显示
            //UnitDisplay = _levelService.GetUnitDisplayString();
            //// 开始事务
            //StartTransaction();
        }

        /// <summary>
        /// 开始Revit事务
        /// </summary>
        private void StartTransaction()
        {
            _transaction = new Transaction(_levelService.Document, "修改标高属性");
            _transaction.Start();
        }

        /// <summary>
        /// 加载标高数据
        /// </summary>
        private void LoadData()
        {
            IsBusy = true;
            StatusMessage = "正在加载标高数据...";

            try
            {
                Levels = _levelService.GetAllLevels();

                // 监听属性变更
                foreach (var level in Levels)
                {
                    level.PropertyChanged += OnLevelPropertyChanged;
                }

                StatusMessage = $"已加载 {Levels.Count} 个标高";
            }
            catch (Exception ex)
            {
                StatusMessage = $"加载失败: {ex.Message}";
                MessageBox.Show($"加载数据时出错:\n{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 监听标高属性变更
        /// </summary>
        private void OnLevelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LevelItem.IsModified))
            {
                HasChanges = Levels.Any(l => l.IsModified);
                StatusMessage = HasChanges ? "有未保存的更改" : "所有更改已保存";
            }
        }

        /// <summary>
        /// 保存所有更改
        /// </summary>
        private async void SaveChanges()
        {
            IsBusy = true;
            StatusMessage = "正在保存更改...";

            try
            {
                var modifiedLevels = Levels.Where(l => l.IsModified).ToList();
                var successCount = 0;

                await System.Threading.Tasks.Task.Run(() =>
                {
                    foreach (var level in modifiedLevels)
                    {
                        if (_levelService.SaveLevel(level))
                        {
                            level.IsModified = false;
                            successCount++;
                        }
                    }
                });

                _transaction.Commit();
                StartTransaction(); // 开始新事务

                StatusMessage = $"成功保存 {successCount} 个标高的修改";
                HasChanges = false;

                if (successCount > 0)
                {
                    MessageBox.Show($"成功保存 {successCount} 个标高", "完成",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"保存失败: {ex.Message}";
                MessageBox.Show($"保存时出错:\n{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 添加新标高
        /// </summary>
        private async void AddNewLevel()
        {
            if (!CanAddLevel()) return;

            IsBusy = true;
            StatusMessage = "正在创建新标高...";

            try
            {
                LevelItem newLevel = null;
                await System.Threading.Tasks.Task.Run(() =>
                {
                    newLevel = _levelService.CreateLevel(NewLevelName, NewLevelElevation);
                });

                if (newLevel != null)
                {
                    newLevel.PropertyChanged += OnLevelPropertyChanged;
                    Levels.Add(newLevel);
                    HasChanges = true;

                    // 清空输入
                    NewLevelName = string.Empty;
                    NewLevelElevation = 0;

                    StatusMessage = $"已创建新标高: {newLevel.Name}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"创建失败: {ex.Message}";
                MessageBox.Show($"创建标高时出错:\n{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 删除选中的标高
        /// </summary>
        private async void DeleteLevel()
        {
            if (SelectedLevel == null) return;

            var result = MessageBox.Show(
                $"确定要删除标高 \"{SelectedLevel.Name}\" 吗？\n此操作不可撤销！",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            IsBusy = true;
            StatusMessage = $"正在删除标高: {SelectedLevel.Name}...";

            try
            {
                bool success = false;
                await System.Threading.Tasks.Task.Run(() =>
                {
                    success = _levelService.DeleteLevel(SelectedLevel.Id);
                });

                if (success)
                {
                    SelectedLevel.PropertyChanged -= OnLevelPropertyChanged;
                    Levels.Remove(SelectedLevel);
                    HasChanges = Levels.Any(l => l.IsModified);
                    StatusMessage = "标高已删除";
                }
                else
                {
                    StatusMessage = "删除失败，请确保标高未被使用";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"删除失败: {ex.Message}";
                MessageBox.Show($"删除标高时出错:\n{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 检查是否可以添加新标高
        /// </summary>
        private bool CanAddLevel() =>
            !IsBusy &&
            !string.IsNullOrWhiteSpace(NewLevelName) &&
            _levelService.IsNameUnique(NewLevelName);

        /// <summary>
        /// 关闭窗口
        /// </summary>
        private void CloseWindow()
        {
            if (HasChanges)
            {
                var result = MessageBox.Show(
                    "有未保存的更改，是否保存？",
                    "提示",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    SaveChanges();
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
            }

            // 回滚未提交的事务
            _transaction?.RollBack();

            var window = System.Windows.Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w.DataContext == this);
            window?.Close();
        }

        public void Dispose()
        {
            _transaction?.Dispose();
        }
    }
    ///// <summary>
    ///// 单位转换服务 - 处理Revit内部单位与显示单位的转换
    ///// </summary>
    //public static class UnitConverter
    //{
    //    // 单位转换比率缓存
    //    private static readonly Dictionary<DisplayUnitType, double> _ratioCache = new Dictionary<DisplayUnitType, double>();
    //    /// <summary>
    //    /// 从显示单位转换为Revit内部单位
    //    /// </summary>
    //    public static double ToAPI(double value, DisplayUnitType sourceUnit)
    //    {
    //        if (sourceUnit == DisplayUnitType.DUT_FAHRENHEIT)
    //        {
    //            return (value + 459.67) / GetRatio(sourceUnit);
    //        }
    //        else if (sourceUnit == DisplayUnitType.DUT_CELSIUS)
    //        {
    //            return value + 273.15;
    //        }
    //        else
    //        {
    //            return value / GetRatio(sourceUnit);
    //        }
    //    }

    //    /// <summary>
    //    /// 从Revit内部单位转换为显示单位
    //    /// </summary>
    //    public static double FromAPI(DisplayUnitType targetUnit, double value)
    //    {
    //        if (targetUnit == DisplayUnitType.DUT_FAHRENHEIT)
    //        {
    //            return value * GetRatio(targetUnit) - 459.67;
    //        }
    //        else if (targetUnit == DisplayUnitType.DUT_CELSIUS)
    //        {
    //            return value - 273.15;
    //        }
    //        else
    //        {
    //            return value * GetRatio(targetUnit);
    //        }
    //    }
    //    /// <summary>
    //    /// 获取单位转换比率（使用缓存提高性能）
    //    /// </summary>
    //    private static double GetRatio(DisplayUnitType unit)
    //    {
    //        if (_ratioCache.TryGetValue(unit, out double cached))
    //            return cached;

    //        double ratio;

    //        switch (unit)
    //        {
    //            case DisplayUnitType.DUT_ACRES:
    //                ratio = 2.29568411386593E-05;
    //                break;
    //            case DisplayUnitType.DUT_CENTIMETERS:
    //                ratio = 30.48;
    //                break;
    //            case DisplayUnitType.DUT_CUBIC_FEET:
    //                ratio = 1;
    //                break;
    //            case DisplayUnitType.DUT_CUBIC_METERS:
    //                ratio = 0.028316846592;
    //                break;
    //            case DisplayUnitType.DUT_DECIMAL_FEET:
    //                ratio = 1;
    //                break;
    //            case DisplayUnitType.DUT_DECIMAL_INCHES:
    //                ratio = 12;
    //                break;
    //            case DisplayUnitType.DUT_FEET_FRACTIONAL_INCHES:
    //                ratio = 1;
    //                break;
    //            case DisplayUnitType.DUT_FRACTIONAL_INCHES:
    //                ratio = 12;
    //                break;
    //            case DisplayUnitType.DUT_METERS:
    //                ratio = 0.3048;
    //                break;
    //            case DisplayUnitType.DUT_MILLIMETERS:
    //                ratio = 304.8;
    //                break;
    //            case DisplayUnitType.DUT_SQUARE_FEET:
    //                ratio = 1;
    //                break;
    //            case DisplayUnitType.DUT_SQUARE_METERS:
    //                ratio = 0.09290304;
    //                break;
    //            default:
    //                ratio = 1;
    //                break;
    //        }

    //        _ratioCache[unit] = ratio;
    //        return ratio;
    //    }
    //}

    //// 向后兼容的Unit类
    //public static class Unit
    //{
    //    public static double ConvertFromAPI(DisplayUnitType to, double value) =>
    //        UnitConverter.FromAPI(to, value);

    //    public static double ConvertToAPI(double value, DisplayUnitType from) =>
    //        UnitConverter.ToAPI(value, from);
    //}
    /// <summary>
    /// 标高数据项 - 单个标高的数据模型
    /// </summary>
    public class LevelItem : INotifyPropertyChanged
    {
        private string _name;
        private double _elevation;
        private int _id;
        private bool _isModified;
        private bool _isNew;

        public int Id
        {
            get => _id;
            set => SetField(ref _id, value);
        }

        public string Name
        {
            get => _name;
            set => SetField(ref _name, value);
        }

        public double Elevation
        {
            get => _elevation;
            set => SetField(ref _elevation, value);
        }

        public bool IsModified
        {
            get => _isModified;
            set => SetField(ref _isModified, value);
        }

        public bool IsNew
        {
            get => _isNew;
            set => SetField(ref _isNew, value);
        }

        public string DisplayElevation => $"{Elevation:F2}";

        public event PropertyChangedEventHandler PropertyChanged;

        protected void SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (propertyName != nameof(IsModified))
                IsModified = true;
        }
    }

    /// <summary>
    /// 标高数据服务 - 封装Revit标高操作
    /// </summary>
    public class LevelService2
    {
        private readonly UIApplication _uiApp;
        private readonly Document _document;
        //private readonly DisplayUnitType _displayUnit;

        public LevelService2(UIApplication uiApp)
        {
            _uiApp = uiApp;
            _document = uiApp.ActiveUIDocument.Document;
            //_displayUnit = _document.GetUnits().GetFormatOptions(UnitType.UT_Length).DisplayUnits;
        }

        /// <summary>
        /// 获取所有标高
        /// </summary>
        public ObservableCollection<LevelItem> GetAllLevels()
        {
            var collector = new FilteredElementCollector(_document);
            var levels = collector.OfClass(typeof(Level))
                .Cast<Level>()
                .Select(ToLevelItem)
                .ToList();

            return new ObservableCollection<LevelItem>(levels);
        }

        /// <summary>
        /// 将Revit Level转换为LevelItem
        /// </summary>
        private LevelItem ToLevelItem(Level level)
        {
            var elevationParam = level.get_Parameter(BuiltInParameter.LEVEL_ELEV);
            var rawElevation = elevationParam.AsDouble();
            //var displayElevation = Unit.ConvertFromAPI(_displayUnit, rawElevation);
            var displayElevation = rawElevation * 304.8;

            return new LevelItem
            {
                Id = level.Id.IntegerValue,
                Name = level.Name,
                Elevation = Math.Round(displayElevation, 2)
            };
        }

        /// <summary>
        /// 保存标高修改
        /// </summary>
        public bool SaveLevel(LevelItem item)
        {
            try
            {
                var levelId = new ElementId(item.Id);
                var level = _document.GetElement(levelId) as Level;
                if (level == null) return false;

                // 更新标高名称
                if (level.Name != item.Name)
                    level.Name = item.Name;

                // 更新标高高度
                var elevationParam = level.get_Parameter(BuiltInParameter.LEVEL_ELEV);
                //var apiValue = Unit.ConvertToAPI(item.Elevation, _displayUnit);
                var apiValue = item.Elevation / 304.8;
                elevationParam.Set(apiValue);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 添加新标高
        /// </summary>
        public LevelItem CreateLevel(string name, double elevation)
        {
            //var apiElevation = Unit.ConvertToAPI(elevation, _displayUnit);
            var apiElevation = elevation / 304.8;
            var newLevel = Level.Create(_document, apiElevation);
            newLevel.Name = name;

            return new LevelItem
            {
                Id = newLevel.Id.IntegerValue,
                Name = newLevel.Name,
                Elevation = elevation,
                IsNew = true
            };
        }

        /// <summary>
        /// 删除标高
        /// </summary>
        public bool DeleteLevel(int levelId)
        {
            try
            {
                var id = new ElementId(levelId);
                _document.Delete(id);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 验证标高名称是否唯一
        /// </summary>
        public bool IsNameUnique(string name, int excludeId = -1) =>
            !GetAllLevels().Any(l => l.Name == name && l.Id != excludeId);
        ///// <summary>
        ///// 获取当前显示单位
        ///// </summary>
        //public string GetUnitDisplayString()
        //{
        //    switch (_displayUnit)
        //    {
        //        case DisplayUnitType.DUT_METERS:
        //            return "m";
        //        case DisplayUnitType.DUT_CENTIMETERS:
        //            return "cm";
        //        case DisplayUnitType.DUT_MILLIMETERS:
        //            return "mm";
        //        case DisplayUnitType.DUT_DECIMAL_FEET:
        //            return "ft";
        //        case DisplayUnitType.DUT_FEET_FRACTIONAL_INCHES:
        //            return "ft";
        //        default:
        //            return "单位";
        //    }
        //}

        public Document Document => _document;
    }


    [ValueConversion(typeof(bool), typeof(string))]
    public class BoolToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                var strings = parameter?.ToString()?.Split(';');
                if (strings?.Length >= 2)
                {
                    return boolValue ? strings[0] : strings[1];
                }
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }

    [ValueConversion(typeof(bool), typeof(Brush))]
    public class BoolToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                var colors = parameter?.ToString()?.Split(';');
                if (colors?.Length >= 2)
                {
                    var colorStr = boolValue ? colors[0] : colors[1];
                    return new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString(colorStr));
                }
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }

    [ValueConversion(typeof(bool), typeof(FontWeight))]
    public class BoolToFontWeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is true ? FontWeights.Bold : FontWeights.Normal;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
