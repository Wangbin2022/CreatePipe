using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// AnalyticalSupportWindow.xaml 的交互逻辑
    /// </summary>
    public partial class AnalyticalSupportWindow : Window
    {
        public AnalyticalSupportWindow()
        {
            InitializeComponent();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 设置关闭回调，使ViewModel能关闭窗口
            if (DataContext is AnalyticalSupportViewModel viewModel)
            {
                viewModel.CloseAction = Close;
            }
        }
    }
    public class AnalyticalSupportViewModel : ObserverableObject
    {
        private readonly ExternalCommandData _commandData;
        private readonly List<ElementId> _selectedIds;
        private ObservableCollection<ElementSupportInfo> _supportInfoList;
        private ElementSupportInfo _selectedItem;

        public AnalyticalSupportViewModel(ExternalCommandData commandData, List<ElementId> selectedIds)
        {
            _commandData = commandData;
            _selectedIds = selectedIds;

            // 加载构件支撑信息
            LoadSupportInfo();

            // 关闭命令
            CloseCommand = new BaseBindingCommand(_ => Close());
        }

        /// <summary>
        /// 构件支撑信息列表
        /// </summary>
        public ObservableCollection<ElementSupportInfo> SupportInfoList
        {
            get => _supportInfoList;
            set { _supportInfoList = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 当前选中的行
        /// </summary>
        public ElementSupportInfo SelectedItem
        {
            get => _selectedItem;
            set { _selectedItem = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 关闭窗口命令
        /// </summary>
        public ICommand CloseCommand { get; }

        /// <summary>
        /// 关闭窗口的回调（由View设置）
        /// </summary>
        public Action CloseAction { get; set; }

        /// <summary>
        /// 加载所有选中构件的支撑信息
        /// </summary>
        private void LoadSupportInfo()
        {
            var doc = _commandData.Application.ActiveUIDocument.Document;
            var infoList = new List<ElementSupportInfo>();

            foreach (var elementId in _selectedIds)
            {
                var element = doc.GetElement(elementId);
                if (element == null) continue;

                // 获取构件的分析模型
                var analyticalModel = element.GetAnalyticalModel();

                // 跳过没有分析模型的构件
                if (analyticalModel == null) continue;

                var info = new ElementSupportInfo
                {
                    Id = element.Id.IntegerValue.ToString(),
                    ElementType = GetElementTypeName(element, doc),
                };

                // 获取支撑信息
                (info.SupportType, info.Remark) = GetSupportInformation(analyticalModel);

                infoList.Add(info);
            }

            SupportInfoList = new ObservableCollection<ElementSupportInfo>(infoList);
        }

        /// <summary>
        /// 获取构件的类型名称（使用switch表达式，C# 8.0特性，C# 7.3使用传统switch）
        /// </summary>
        private string GetElementTypeName(Element element, Document doc)
        {
            switch (element)
            {
                case WallFoundation wallFound:
                    var footSymbol = doc.GetElement(wallFound.GetTypeId()) as ElementType;
                    return $"{footSymbol?.Category?.Name}: {footSymbol?.Name}";

                case FamilyInstance familyInstance:
                    var symbol = doc.GetElement(familyInstance.GetTypeId()) as FamilySymbol;
                    return $"{symbol?.Family?.Name}: {symbol?.Name}";

                case Floor slab:
                    var slabType = doc.GetElement(slab.GetTypeId()) as FloorType;
                    return $"{slabType?.Category?.Name}: {slabType?.Name}";

                case Wall wall:
                    var wallType = doc.GetElement(wall.GetTypeId()) as WallType;
                    return $"{wallType?.Kind}: {wallType?.Name}";

                default:
                    return element.GetType().Name;
            }
        }

        /// <summary>
        /// 获取构件的支撑信息
        /// </summary>
        /// <returns>(支撑类型, 备注)</returns>
        private (string SupportType, string Remark) GetSupportInformation(AnalyticalModel analyticalModel)
        {
            // 使用元组语法（C# 7.0特性）
            string supportType = "";
            string remark = "";

            // 获取所有支撑
            var supports = analyticalModel.GetAnalyticalModelSupports();

            // 判断是否完全支撑
            var isFullySupported = analyticalModel.IsElementFullySupported();

            if (!isFullySupported)
            {
                if (supports.Count == 0)
                {
                    supportType = "未支撑";
                }
                else
                {
                    // 拼接所有支撑类型
                    supportType = string.Join(", ", supports.Select(s => s.GetSupportType().ToString()));
                }
            }
            else
            {
                if (supports.Count == 0)
                {
                    remark = "已完全支撑，但无详细信息";
                }
                else
                {
                    supportType = string.Join(", ", supports.Select(s => s.GetSupportType().ToString()));
                }
            }

            return (supportType, remark);
        }

        private void Close()
        {
            CloseAction?.Invoke();
        }
    }
    /// <summary>
    /// 构件支撑信息模型
    /// </summary>
    public class ElementSupportInfo : ObserverableObject
    {
        private string _id;
        private string _elementType;
        private string _supportType;
        private string _remark;

        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string ElementType
        {
            get => _elementType;
            set { _elementType = value; OnPropertyChanged(); }
        }

        public string SupportType
        {
            get => _supportType;
            set { _supportType = value; OnPropertyChanged(); }
        }

        public string Remark
        {
            get => _remark;
            set { _remark = value; OnPropertyChanged(); }
        }
    }
}
