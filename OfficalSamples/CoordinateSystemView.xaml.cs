using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// CoordinateSystemView.xaml 的交互逻辑
    /// </summary>
    public partial class CoordinateSystemView : Window
    {
        public CoordinateSystemView(ExternalCommandData commandData)
        {
            InitializeComponent();
            this.DataContext = new CoordinateSystemViewModel(commandData);
        }

    }
    /// <summary>
    /// 主窗口ViewModel - 协调视图与模型的交互
    /// </summary>
    public class CoordinateSystemViewModel : ObserverableObject
    {
        private readonly ProjectLocationService _locationService;
        private readonly ExternalCommandData _commandData;

        // 当前选中的位置
        private ProjectLocationModel _selectedLocation;
        // 编辑中的偏移量（临时存储，确认后提交）
        private double _editAngle;
        private double _editEastWest;
        private double _editNorthSouth;
        private double _editElevation;
        // 新位置名称（用于复制）
        private string _newLocationName;
        // 是否正在加载
        private bool _isLoading = true;

        /// <summary>
        /// 所有项目位置集合
        /// </summary>
        public ObservableCollection<ProjectLocationModel> Locations { get; private set; }

        /// <summary>
        /// 当前选中的位置
        /// </summary>
        public ProjectLocationModel SelectedLocation
        {
            get { return _selectedLocation; }
            set
            {
                _selectedLocation = value;
                OnPropertyChanged();
                // 选中变化时更新编辑值和命令状态
                if (value != null)
                {
                    EditAngle = value.Angle;
                    EditEastWest = value.EastWest;
                    EditNorthSouth = value.NorthSouth;
                    EditElevation = value.Elevation;
                }
                // 通知命令可执行状态变化
                ((BaseBindingCommand)MakeCurrentCommand).RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// 编辑角度
        /// </summary>
        public double EditAngle
        {
            get { return _editAngle; }
            set { _editAngle = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 编辑东西偏移
        /// </summary>
        public double EditEastWest
        {
            get { return _editEastWest; }
            set { _editEastWest = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 编辑南北偏移
        /// </summary>
        public double EditNorthSouth
        {
            get { return _editNorthSouth; }
            set { _editNorthSouth = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 编辑高程
        /// </summary>
        public double EditElevation
        {
            get { return _editElevation; }
            set { _editElevation = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 新位置名称（复制用）
        /// </summary>
        public string NewLocationName
        {
            get { return _newLocationName; }
            set { _newLocationName = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 是否显示"设为当前"按钮
        /// </summary>
        public bool CanMakeCurrent
        {
            get { return SelectedLocation != null && !SelectedLocation.IsCurrent; }
        }

        /// <summary>
        /// 命令：设为当前位置
        /// </summary>
        public ICommand MakeCurrentCommand;
        /// <summary>
        /// 命令：复制位置
        /// </summary>
        public ICommand DuplicateCommand;
        /// <summary>
        /// 命令：保存修改
        /// </summary>
        public ICommand SaveCommand;
        /// <summary>
        /// 命令：取消/关闭
        /// </summary>
        public ICommand CancelCommand;

        public CoordinateSystemViewModel(ExternalCommandData commandData)
        {
            _commandData = commandData;
            _locationService = new ProjectLocationService(commandData);
            Locations = new ObservableCollection<ProjectLocationModel>();

            // 初始化命令
            MakeCurrentCommand = new BaseBindingCommand(ExecuteMakeCurrent, CanExecuteMakeCurrent);
            DuplicateCommand = new BaseBindingCommand(ExecuteDuplicate);
            SaveCommand = new BaseBindingCommand(ExecuteSave);
            CancelCommand = new BaseBindingCommand(ExecuteCancel);

            // 加载数据
            LoadLocations();
        }

        /// <summary>
        /// 加载所有项目位置
        /// </summary>
        private void LoadLocations()
        {
            Locations.Clear();
            List<ProjectLocationModel> locations = _locationService.GetAllLocations();
            foreach (ProjectLocationModel loc in locations)
            {
                Locations.Add(loc);
            }

            // 默认选中当前位置
            ProjectLocationModel current = locations.FirstOrDefault(l => l.IsCurrent);
            if (current != null)
            {
                SelectedLocation = current;
            }
            else if (Locations.Count > 0)
            {
                SelectedLocation = Locations[0];
            }

            _isLoading = false;
        }

        /// <summary>
        /// 执行：设为当前位置
        /// </summary>
        private void ExecuteMakeCurrent(object parameter)
        {
            if (SelectedLocation == null || SelectedLocation.IsCurrent) return;

            try
            {
                // 启动事务
                Transaction trans = new Transaction(
                    _commandData.Application.ActiveUIDocument.Document,
                    "设置当前项目位置");
                trans.Start();

                _locationService.SetCurrentLocation(SelectedLocation.Name);

                trans.Commit();

                // 刷新列表，更新当前状态
                RefreshLocations();
            }
            catch (Exception ex)
            {
                MessageBox.Show("设置当前位置失败: " + ex.Message, "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 判断是否可以设为当前
        /// </summary>
        private bool CanExecuteMakeCurrent(object parameter)
        {
            return SelectedLocation != null && !SelectedLocation.IsCurrent;
        }

        /// <summary>
        /// 执行：复制位置
        /// </summary>
        private void ExecuteDuplicate(object parameter)
        {
            if (SelectedLocation == null) return;

            // 弹出输入对话框获取新名称
            string newName = ShowDuplicateDialog(SelectedLocation.Name);
            if (string.IsNullOrEmpty(newName)) return;

            try
            {
                Transaction trans = new Transaction(
                    _commandData.Application.ActiveUIDocument.Document,
                    "复制项目位置");
                trans.Start();

                _locationService.DuplicateLocation(SelectedLocation.Name, newName);

                trans.Commit();

                // 刷新并选中新位置
                RefreshLocations();
                ProjectLocationModel newLoc = Locations.FirstOrDefault(l => l.Name == newName);
                if (newLoc != null)
                {
                    SelectedLocation = newLoc;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("复制位置失败: " + ex.Message, "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 执行：保存所有修改
        /// </summary>
        private void ExecuteSave(object parameter)
        {
            if (SelectedLocation == null) return;

            try
            {
                Transaction trans = new Transaction(
                    _commandData.Application.ActiveUIDocument.Document,
                    "修改项目位置");
                trans.Start();

                // 保存偏移量修改
                _locationService.EditPosition(
                    SelectedLocation.Name,
                    EditAngle,
                    EditEastWest,
                    EditNorthSouth,
                    EditElevation);

                trans.Commit();

                // 刷新显示
                RefreshLocations();
            }
            catch (FormatException)
            {
                MessageBox.Show("请输入有效的数字", "输入错误",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存失败: " + ex.Message, "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 执行：取消关闭
        /// </summary>
        private void ExecuteCancel(object parameter)
        {
            // 由View处理关闭窗口
        }

        /// <summary>
        /// 刷新位置列表
        /// </summary>
        private void RefreshLocations()
        {
            string selectedName = SelectedLocation != null ? SelectedLocation.Name : null;
            LoadLocations();

            // 恢复选中
            if (!string.IsNullOrEmpty(selectedName))
            {
                ProjectLocationModel loc = Locations.FirstOrDefault(l => l.Name == selectedName);
                if (loc != null)
                {
                    SelectedLocation = loc;
                }
            }
        }

        /// <summary>
        /// 显示复制对话框（简化实现，实际可用独立Window）
        /// </summary>
        private string ShowDuplicateDialog(string sourceName)
        {
            // 使用简单的输入对话框
            string defaultName = sourceName + "_副本";
            CoordinateSystemInputDialog dialog
                = new CoordinateSystemInputDialog("复制位置", "请输入新位置名称:", defaultName);
            if (dialog.ShowDialog() == true)
            {
                return dialog.InputText;
            }
            return null;
        }
    }
    /// <summary>
    /// Revit项目位置数据服务 - 封装所有与Revit API的交互
    /// </summary>
    public class ProjectLocationService
    {
        private readonly ExternalCommandData _commandData;
        private readonly UIApplication _uiApplication;
        private readonly Document _document;

        // 角度转弧度的系数
        private const double DegreeToRadian = 0.0174532925199433;
        // 默认精度
        private const int Precision = 3;

        public ProjectLocationService(ExternalCommandData commandData)
        {
            _commandData = commandData;
            _uiApplication = commandData.Application;
            _document = commandData.Application.ActiveUIDocument.Document;
        }

        /// <summary>
        /// 获取所有项目位置
        /// </summary>
        public List<ProjectLocationModel> GetAllLocations()
        {
            List<ProjectLocationModel> locations = new List<ProjectLocationModel>();
            ProjectLocation currentLocation = _document.ActiveProjectLocation;
            string currentName = currentLocation.Name;

            ProjectLocationSet projectLocations = _document.ProjectLocations;
            ProjectLocationSetIterator iter = projectLocations.ForwardIterator();
            iter.Reset();

            while (iter.MoveNext())
            {
                ProjectLocation location = iter.Current as ProjectLocation;
                if (location == null) continue;

                // 获取该位置的偏移量
                XYZ origin = new XYZ(0, 0, 0);
                ProjectPosition position = location.GetProjectPosition(origin);

                ProjectLocationModel model = new ProjectLocationModel
                {
                    Name = location.Name,
                    IsCurrent = (location.Name == currentName),
                    Angle = Round(position.Angle / DegreeToRadian),
                    EastWest = Round(position.EastWest),
                    NorthSouth = Round(position.NorthSouth),
                    Elevation = Round(position.Elevation)
                };

                locations.Add(model);
            }

            return locations;
        }

        /// <summary>
        /// 切换当前项目位置
        /// </summary>
        public void SetCurrentLocation(string locationName)
        {
            ProjectLocationSet locations = _document.ProjectLocations;
            foreach (ProjectLocation projectLocation in locations)
            {
                if (projectLocation.Name == locationName)
                {
                    _document.ActiveProjectLocation = projectLocation;
                    break;
                }
            }
        }

        /// <summary>
        /// 复制项目位置
        /// </summary>
        public void DuplicateLocation(string sourceName, string newName)
        {
            ProjectLocationSet locationSet = _document.ProjectLocations;
            foreach (ProjectLocation projectLocation in locationSet)
            {
                if (projectLocation.Name == sourceName ||
                    projectLocation.Name + " (current)" == sourceName)
                {
                    projectLocation.Duplicate(newName);
                    break;
                }
            }
        }

        /// <summary>
        /// 编辑位置偏移量
        /// </summary>
        public void EditPosition(string locationName, double angle, double eastWest,
                                 double northSouth, double elevation)
        {
            ProjectLocationSet locationSet = _document.ProjectLocations;
            foreach (ProjectLocation location in locationSet)
            {
                if (location.Name == locationName ||
                    location.Name + " (当前)" == locationName)
                {
                    XYZ origin = new XYZ(0, 0, 0);
                    ProjectPosition projectPosition = location.GetProjectPosition(origin);

                    // 将度转换为弧度存储
                    projectPosition.Angle = angle * DegreeToRadian;
                    projectPosition.EastWest = eastWest;
                    projectPosition.NorthSouth = northSouth;
                    projectPosition.Elevation = elevation;

                    location.SetProjectPosition(origin, projectPosition);
                    break;
                }
            }
        }

        /// <summary>
        /// 保存场地位置信息（经纬度、时区）
        /// </summary>
        public void SaveSiteLocation(double latitude, double longitude, double timeZone)
        {
            SiteLocation siteLocation = _document.SiteLocation;
            if (siteLocation == null) return;

            siteLocation.Latitude = latitude;
            siteLocation.Longitude = longitude;
            siteLocation.TimeZone = timeZone;
        }

        /// <summary>
        /// 获取当前场地位置
        /// </summary>
        public SiteLocation GetSiteLocation()
        {
            return _document.SiteLocation;
        }

        /// <summary>
        /// 获取城市集合
        /// </summary>
        public CitySet GetCities()
        {
            return _uiApplication.Application.Cities;
        }

        /// <summary>
        /// 数值精度处理
        /// </summary>
        private static double Round(double value)
        {
            return Math.Round(value, Precision);
        }
    }
    /// <summary>
    /// 项目位置信息模型 - 封装单个项目位置的完整数据
    /// </summary>
    public class ProjectLocationModel : INotifyPropertyChanged
    {
        private string _name;
        private bool _isCurrent;
        private double _angle;        // 真北偏角（度）
        private double _eastWest;     // 东西偏移
        private double _northSouth;   // 南北偏移
        private double _elevation;    // 高程

        /// <summary>
        /// 位置名称
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 是否为当前激活位置
        /// </summary>
        public bool IsCurrent
        {
            get { return _isCurrent; }
            set { _isCurrent = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 真北偏角（度）
        /// </summary>
        public double Angle
        {
            get { return _angle; }
            set { _angle = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 东西向偏移
        /// </summary>
        public double EastWest
        {
            get { return _eastWest; }
            set { _eastWest = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 南北向偏移
        /// </summary>
        public double NorthSouth
        {
            get { return _northSouth; }
            set { _northSouth = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 高程
        /// </summary>
        public double Elevation
        {
            get { return _elevation; }
            set { _elevation = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 显示名称（当前位置带标记）
        /// </summary>
        public string DisplayName
        {
            get { return IsCurrent ? Name + " (当前)" : Name; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            // 名称变化时联动更新显示名称
            if (propertyName == nameof(Name) || propertyName == nameof(IsCurrent))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayName)));
            }
        }
    }

    /// <summary>
    /// 城市信息模型
    /// </summary>
    public class CityInfoModel : ObserverableObject
    {
        private string _cityName;
        private double _latitude;   // 纬度
        private double _longitude;  // 经度
        private double _timeZone;   // 时区

        public string CityName
        {
            get { return _cityName; }
            set { _cityName = value; OnPropertyChanged(); }
        }

        public double Latitude
        {
            get { return _latitude; }
            set { _latitude = value; OnPropertyChanged(); }
        }

        public double Longitude
        {
            get { return _longitude; }
            set { _longitude = value; OnPropertyChanged(); }
        }

        public double TimeZone
        {
            get { return _timeZone; }
            set { _timeZone = value; OnPropertyChanged(); }
        }
    }
}
