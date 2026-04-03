using CreatePipe.cmd;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace CreatePipe.Form
{
    /// <summary>
    /// ColorPickerDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ColorPickerDialog : Window
    {
        public Color SelectedColor { get; private set; }
        public ColorPickerDialog(Color? initialColor = null)
        {
            InitializeComponent();
            DataContext = new ColorPickerViewModel(initialColor ?? Colors.White);
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ColorPickerViewModel vm)
            {
                SelectedColor = vm.CurrentColor;
                DialogResult = true;
            }
            Close();
        }
    }
    public class ColorPickerViewModel : ObserverableObject
    {
        public ColorPickerViewModel(Color initialColor)
        {
            Red = initialColor.R;
            Green = initialColor.G;
            Blue = initialColor.B;
            PreviewColorBrush = new SolidColorBrush(initialColor);

            SelectPresetColorCommand = new RelayCommand<Color>(color =>
            {
                Red = color.R;
                Green = color.G;
                Blue = color.B;
            });
        }
        private void UpdatePreviewColor()
        {
            PreviewColorBrush = new SolidColorBrush(CurrentColor);
        }
        public ICommand ConfirmCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand SelectPresetColorCommand { get; }
        //    public ObservableCollection<Color> PresetColors { get; } = new()
        //{
        //    Colors.Red, Colors.Green, Colors.Blue,
        //    Colors.Yellow, Colors.Cyan, Colors.Magenta,
        //    Colors.White, Colors.Black, Colors.Gray,
        //    Colors.Orange, Colors.Purple, Colors.Pink,
        //    Colors.Brown, Colors.LightBlue, Colors.LightGreen
        //};
        private byte _red;
        public byte Red
        {
            get => _red;
            set { _red = value; OnPropertyChanged(); UpdatePreviewColor(); }
        }
        private byte _green;
        public byte Green
        {
            get => _green;
            set { _green = value; OnPropertyChanged(); UpdatePreviewColor(); }
        }
        private byte _blue;
        public byte Blue
        {
            get => _blue;
            set { _blue = value; OnPropertyChanged(); UpdatePreviewColor(); }
        }
        public Color CurrentColor => Color.FromRgb(Red, Green, Blue);

        private SolidColorBrush _previewColorBrush;
        public SolidColorBrush PreviewColorBrush
        {
            get => _previewColorBrush;
            set { _previewColorBrush = value; OnPropertyChanged(); }
        }

    }
}
