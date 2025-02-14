using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using System.ComponentModel;
using System.Windows.Media;

namespace CreatePipe.PipeSystemManager.Entity
{
    /// <summary>
    /// 管道系统实体类
    /// </summary>
    public class PipeSystemEntity : INotifyPropertyChanged
    {
        /// <summary>
        /// 系统类型对象
        /// </summary>
        public PipingSystemType PipingSystemType { get; set; }
        private string systemName;
        private string abbreviation;
        private int lineWeight;
        private LinePatternElement linePatternElement;
        private SolidColorBrush solidColorBrush;
        public MEPSystemTypeEntity PipeSystemTypeEntity { get; set; }

        /// <summary>
        /// 是修改的 默认是false
        /// </summary>
        public bool IsUpdate { get; set; }

        /// <summary>
        /// 系统名称
        /// </summary>
        public string SystemName
        {
            get { return systemName; }
            set { systemName = value; OnPropertyChanged("SystemName"); }
        }

        /// <summary>
        /// 缩写
        /// </summary>
        public string Abbreviation
        {
            get { return abbreviation; }
            set { abbreviation = value; OnPropertyChanged("Abbreviation"); }
        }

        public int LineWeight
        {
            get { return lineWeight; }
            set { lineWeight = value; OnPropertyChanged("LineWeight"); }
        }

        /// <summary>
        /// 线型
        /// </summary>
        public LinePatternElement LinePatternElement { get => linePatternElement; set => linePatternElement = value; }

        /// <summary>
        /// 颜色
        /// </summary>
        public SolidColorBrush SolidColorBrush { get => solidColorBrush; set => solidColorBrush = value; }
        public event PropertyChangedEventHandler PropertyChanged;
        protected internal virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }


}
