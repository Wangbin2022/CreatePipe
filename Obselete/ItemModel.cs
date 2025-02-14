using CreatePipe.cmd;

namespace CreatePipe
{
    public class ItemModel : ObserverableObject
    {
        private bool _button2Enabled = false;
        public bool Button2Enabled
        {
            get => _button2Enabled;
            set
            {
                if (_button2Enabled != value)
                {
                    _button2Enabled = value;
                    OnPropertyChanged(nameof(Button2Enabled));
                }
            }
        }
        public void OnButton1Click()
        {
            this.Button2Enabled = true; // 点击按钮1时，使Button2可用
        }
        public string Name { get; set; }
    }
}
