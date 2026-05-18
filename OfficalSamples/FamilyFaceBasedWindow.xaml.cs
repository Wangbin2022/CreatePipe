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
    /// FamilyFaceBasedWindow.xaml 的交互逻辑
    /// </summary>
    public partial class FamilyFaceBasedWindow : Window
    {
        public FamilyFaceBasedWindow()
        {
            InitializeComponent();
        }
    }
    /// <summary>
    /// FaceBased模式ViewModel - 选择族符号和放置方式
    /// </summary>
    public class FaceBasedViewModel : ObserverableObject
    {
        private FamilySymbolItem3 _selectedFamilySymbol;
        private FaceBasedPlacementOption _selectedOption = FaceBasedPlacementOption.PlaceOnFace;

        public ObservableCollection<FamilySymbolItem3> FamilySymbols { get; }

        public FamilySymbolItem3 SelectedFamilySymbol
        {
            get => _selectedFamilySymbol;
            set { _selectedFamilySymbol = value; OnPropertyChanged(); }
        }

        public FaceBasedPlacementOption SelectedOption
        {
            get => _selectedOption;
            set { _selectedOption = value; OnPropertyChanged(); }
        }

        public bool IsDefaultSelected
        {
            get => _selectedOption == FaceBasedPlacementOption.Default;
            set { if (value) SelectedOption = FaceBasedPlacementOption.Default; }
        }

        public bool IsPlaceOnFaceSelected
        {
            get => _selectedOption == FaceBasedPlacementOption.PlaceOnFace;
            set { if (value) SelectedOption = FaceBasedPlacementOption.PlaceOnFace; }
        }

        public bool IsPlaceOnVerticalFaceSelected
        {
            get => _selectedOption == FaceBasedPlacementOption.PlaceOnVerticalFace;
            set { if (value) SelectedOption = FaceBasedPlacementOption.PlaceOnVerticalFace; }
        }

        public bool IsPlaceOnWorkPlaneSelected
        {
            get => _selectedOption == FaceBasedPlacementOption.PlaceOnWorkPlane;
            set { if (value) SelectedOption = FaceBasedPlacementOption.PlaceOnWorkPlane; }
        }

        public ICommand OkCommand { get; }
        public ICommand CancelCommand { get; }

        public event Action<FamilySymbol> PlacementConfirmed;
        //public event Action<FamilySymbol,FaceBasedPlacementOptions> PlacementConfirmed;

        public FaceBasedViewModel(ObservableCollection<FamilySymbolItem3> familySymbols)
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

            //var options = new FaceBasedPlacementOptions
            //{
            //    PlacementType = (Autodesk.Revit.UI.FaceBasedPlacementType)_selectedOption
            //};
            //PlacementConfirmed?.Invoke(SelectedFamilySymbol.Symbol, options);
            PlacementConfirmed?.Invoke(SelectedFamilySymbol.Symbol);
            CloseWindow?.Invoke();
        }

        public Action CloseWindow { get; set; }
    }
}
