using CreatePipe.cmd;

namespace CreatePipe.models
{
    public class ProgressBarViewModel : ObserverableObject
    {
        public ProgressBarViewModel()
        {
            //不使用消息中心如何实现？
            System.Windows.Forms.Application.DoEvents();//更新窗口
            //Title =$"{Value}/{Maximum}_{Title}";

        }
        private int _maxmium;

        public int Maxmium
        {
            get { return _maxmium; }
            set
            {
                _maxmium = value;
                OnPropertyChanged();
            }
        }
        private int _value;
        public int Value
        {
            get { return _value; }
            set
            {
                _value = value;
                OnPropertyChanged();
            }
        }
        private string _title;
        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged();
            }
        }


    }
}
