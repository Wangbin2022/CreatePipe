using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// FamilyPlacementBy.xaml 的交互逻辑
    /// </summary>
    public partial class FamilySketchBasedWindow : Window
    {
        public FamilySketchBasedWindow()
        {
            InitializeComponent();
        }
    }
    /// <summary>
    /// SketchBased模式ViewModel - 选择族符号和草图工具
    /// </summary>
    public class SketchBasedViewModel : ObserverableObject
    {
        private FamilySymbolItem3 _selectedFamilySymbol;
        private SketchToolOption _selectedTool = SketchToolOption.Line;

        public ObservableCollection<FamilySymbolItem3> FamilySymbols { get; }

        public FamilySymbolItem3 SelectedFamilySymbol
        {
            get => _selectedFamilySymbol;
            set { _selectedFamilySymbol = value; OnPropertyChanged(); }
        }

        public SketchToolOption SelectedTool
        {
            get => _selectedTool;
            set { _selectedTool = value; OnPropertyChanged(); }
        }

        // 为每个草图工具提供独立绑定属性
        public bool IsLineSelected
        {
            get => _selectedTool == SketchToolOption.Line;
            set { if (value) SelectedTool = SketchToolOption.Line; }
        }

        public bool IsArc3PointSelected
        {
            get => _selectedTool == SketchToolOption.Arc3Point;
            set { if (value) SelectedTool = SketchToolOption.Arc3Point; }
        }

        public bool IsArcCenterEndsSelected
        {
            get => _selectedTool == SketchToolOption.ArcCenterEnds;
            set { if (value) SelectedTool = SketchToolOption.ArcCenterEnds; }
        }

        public bool IsSplineSelected
        {
            get => _selectedTool == SketchToolOption.Spline;
            set { if (value) SelectedTool = SketchToolOption.Spline; }
        }

        public bool IsPartialEllipseSelected
        {
            get => _selectedTool == SketchToolOption.PartialEllipse;
            set { if (value) SelectedTool = SketchToolOption.PartialEllipse; }
        }

        public bool IsPickLinesSelected
        {
            get => _selectedTool == SketchToolOption.PickLines;
            set { if (value) SelectedTool = SketchToolOption.PickLines; }
        }

        public ICommand OkCommand { get; }
        public ICommand CancelCommand { get; }

        public event Action<FamilySymbol, SketchGalleryOptions> PlacementConfirmed;

        public SketchBasedViewModel(ObservableCollection<FamilySymbolItem3> familySymbols)
        {
            FamilySymbols = familySymbols;
            SelectedFamilySymbol = familySymbols.FirstOrDefault();

            OkCommand = new BaseBindingCommand(_ => OnOk());
            CancelCommand = new BaseBindingCommand(_ => CloseWindow?.Invoke());
        }

        private void OnOk()
        {
            if (SelectedFamilySymbol?.Symbol == null)
            {
                TaskDialog.Show("错误", "请选择一个族符号。");
                return;
            }

            //var options = new SketchGalleryOptions
            //{
            //    SketchGalleryType = (SketchGalleryType)_selectedTool
            //};
            var options = new SketchGalleryOptions();
            PlacementConfirmed?.Invoke(SelectedFamilySymbol.Symbol, options);
            CloseWindow?.Invoke();
        }
        public Action CloseWindow { get; set; }

    }
}
