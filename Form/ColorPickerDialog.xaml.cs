using CreatePipe.cmd;
using System.Collections.ObjectModel;
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
            Color defaultColor = Color.FromRgb(127, 127, 127);
            DataContext = new ColorPickerViewModel(initialColor ?? defaultColor);
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
            _red = initialColor.R;
            _green = initialColor.G;
            _blue = initialColor.B;
            UpdatePreviewColor();

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
        public ICommand SelectPresetColorCommand { get; }

        // 恢复预设颜色集合，否则界面下半部分是空白的
        public ObservableCollection<Color> PresetColors { get; } = new ObservableCollection<Color>
        {
            //Colors.Red, Colors.Green, Colors.Blue,
            //Colors.Yellow, Colors.Cyan, Colors.Magenta,
            //Colors.White, Colors.Black, Colors.Gray,
            //Colors.Orange, Colors.Purple, Colors.Pink,
            //Colors.Brown, Colors.LightBlue, Colors.LightGreen
        };
        private byte _red;
        public byte Red
        {
            get => _red;
            set
            {
                // 必须加判断，避免双向绑定导致无限循环和输入框光标跳动
                if (_red != value)
                {
                    _red = value;
                    OnPropertyChanged();
                    UpdatePreviewColor();
                }
            }
        }
        private byte _green;
        public byte Green
        {
            get => _green;
            set
            {
                if (_green != value)
                {
                    _green = value;
                    OnPropertyChanged();
                    UpdatePreviewColor();
                }
            }
        }
        private byte _blue;
        public byte Blue
        {
            get => _blue;
            set
            {
                if (_blue != value)
                {
                    _blue = value;
                    OnPropertyChanged();
                    UpdatePreviewColor();
                }
            }
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
