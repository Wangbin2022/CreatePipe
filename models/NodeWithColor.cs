using CreatePipe.cmd;

namespace CreatePipe.models
{
    public class NodeWithColor : ObserverableObject
    {
        public string Title { get; set; }
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
        public Autodesk.Revit.DB.Color LayerColor { get; set; }

        public NodeWithColor(string title, Autodesk.Revit.DB.Color color = null)
        {
            Title = title;
            LayerColor = color;
        }
    }
}
