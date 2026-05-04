using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using static CreatePipe.Form.CircleGaugePlaceViewModel;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// FamilyPlacementView.xaml 的交互逻辑
    /// </summary>
    public partial class FamilyPlacementView : Window
    {
        public FamilyPlacementView(ExternalCommandData commandData)
        {
            InitializeComponent();
            var viewModel = new FamilyPlacementViewModel(commandData);
            viewModel.CloseWindow = Close;
            DataContext = viewModel;
        }
    }
    /// <summary>
    /// 主窗口ViewModel - 管理放置类型选择和流程控制
    /// </summary>
    public class FamilyPlacementViewModel : ObserverableObject
    {
        private readonly ExternalCommandData _commandData;
        private PlacementType _selectedPlacementType = PlacementType.FaceBased;
        private bool _isProcessing;

        public PlacementType SelectedPlacementType
        {
            get => _selectedPlacementType;
            set { _selectedPlacementType = value; OnPropertyChanged(); }
        }

        public bool IsFaceBasedSelected
        {
            get => _selectedPlacementType == PlacementType.FaceBased;
            set { if (value) SelectedPlacementType = PlacementType.FaceBased; }
        }

        public bool IsSketchBasedSelected
        {
            get => _selectedPlacementType == PlacementType.SketchBased;
            set { if (value) SelectedPlacementType = PlacementType.SketchBased; }
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set { _isProcessing = value; OnPropertyChanged(); }
        }

        public ICommand NextCommand { get; }
        public ICommand CancelCommand { get; }

        public FamilyPlacementViewModel(ExternalCommandData commandData)
        {
            _commandData = commandData;
            NextCommand = new BaseBindingCommand(_ => OnNext());
            CancelCommand = new BaseBindingCommand(_ => CloseWindow?.Invoke());
        }

        private void OnNext()
        {
            IsProcessing = true;
            try
            {
                if (_selectedPlacementType == PlacementType.FaceBased)
                {
                    ShowFaceBasedDialog();
                }
                else
                {
                    ShowSketchBasedDialog();
                }
            }
            finally
            {
                IsProcessing = false;
                CloseWindow?.Invoke();
            }
        }

        private void ShowFaceBasedDialog()
        {
            var famSymbols = FindFamilySymbols(_commandData, BuiltInCategory.OST_GenericModel);
            if (famSymbols.Count == 0)
            {
                TaskDialog.Show("错误", "没有找到基于面的族符号，请先加载一个。");
                return;
            }

            var dialogVm = new FaceBasedViewModel(famSymbols);
            var window = new FamilyFaceBasedWindow { DataContext = dialogVm };
            dialogVm.CloseWindow = () => window.Close();
            dialogVm.PlacementConfirmed += (symbol) =>
            {
                //_commandData.Application.ActiveUIDocument.PromptForFamilyInstancePlacement(symbol, options);
                _commandData.Application.ActiveUIDocument.PromptForFamilyInstancePlacement(symbol);
            };
            window.ShowDialog();
        }

        private void ShowSketchBasedDialog()
        {
            var famSymbols = FindFamilySymbols(_commandData, BuiltInCategory.OST_StructuralFraming);
            if (famSymbols.Count == 0)
            {
                TaskDialog.Show("错误", "没有找到基于草图的族符号，请先加载一个。");
                return;
            }

            var dialogVm = new SketchBasedViewModel(famSymbols);
            var window = new FamilySketchBasedWindow { DataContext = dialogVm };
            dialogVm.CloseWindow = () => window.Close();
            dialogVm.PlacementConfirmed += (symbol, options) =>
            {
                //_commandData.Application.ActiveUIDocument.PromptForFamilyInstancePlacement(symbol, options);
                _commandData.Application.ActiveUIDocument.PromptForFamilyInstancePlacement(symbol);
            };
            window.ShowDialog();
        }

        private static ObservableCollection<FamilySymbolItem3> FindFamilySymbols(
            ExternalCommandData commandData, BuiltInCategory category)
        {
            var doc = commandData.Application.ActiveUIDocument.Document;
            var collector = new FilteredElementCollector(doc);
            var filter = new ElementCategoryFilter(category);

            var symbols = collector.WherePasses(filter)
                .Cast<FamilySymbol>()
                .Where(s => s != null)
                .Select(s => new FamilySymbolItem3
                {
                    DisplayName = $"{s.Family.Name} : {s.Name}",
                    Symbol = s
                })
                .ToList();

            return new ObservableCollection<FamilySymbolItem3>(symbols);
        }

        public Action CloseWindow { get; set; }
    }
    /// <summary>
    /// 放置类型枚举
    /// </summary>
    public enum PlacementType
    {
        FaceBased,   // 基于面的放置
        SketchBased  // 基于草图的放置
    }

    /// <summary>
    /// FaceBased模式放置方式枚举
    /// </summary>
    public enum FaceBasedPlacementOption
    {
        Default,           // 默认
        PlaceOnFace,       // 放置在面上
        PlaceOnVerticalFace, // 放置在垂直面上
        PlaceOnWorkPlane   // 放置在工作平面上
    }

    /// <summary>
    /// SketchBased模式草图工具枚举
    /// </summary>
    public enum SketchToolOption
    {
        Line,           // 直线
        Arc3Point,      // 三点圆弧
        ArcCenterEnds,  // 圆心-端点圆弧
        Spline,         // 样条曲线
        PartialEllipse, // 部分椭圆
        PickLines       // 拾取线
    }

    /// <summary>
    /// 族符号项ViewModel
    /// </summary>
    public class FamilySymbolItem3
    {
        public string DisplayName { get; set; }
        public FamilySymbol Symbol { get; set; }
    }
}
