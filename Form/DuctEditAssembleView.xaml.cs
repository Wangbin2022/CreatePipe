using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CreatePipe.cmd;
using CreatePipe.filter;
using CreatePipe.Utils;
using System.Windows;
using System.Windows.Input;

namespace CreatePipe.Form
{
    /// <summary>
    /// DuctEditAssembleView.xaml 的交互逻辑
    /// </summary>
    public partial class DuctEditAssembleView : Window
    {
        // 记录当前显示了几行（默认显示1行）
        private int _visibleRowCount = 1;
        // 界面最大允许显示的行数
        private readonly int _maxRowCount = 1;
        public DuctEditAssembleView(UIApplication uiApp)
        {
            InitializeComponent();
            this.DataContext = new DuctEditAssembleViewModel(uiApp);
            // 窗体初始化时，刷新一次显示状态，隐藏多余的行
            UpdateRowsVisibility();
        }
        // 第一行最后一个按钮（加号）点击事件
        private void BtnAddRow_Click(object sender, RoutedEventArgs e)
        {
            if (_visibleRowCount < _maxRowCount)
            {
                _visibleRowCount++;
                UpdateRowsVisibility();
            }
        }
        // 第二行最后一个按钮（减号）点击事件
        private void BtnRemoveRow_Click(object sender, RoutedEventArgs e)
        {
            if (_visibleRowCount > 1)
            {
                _visibleRowCount--;
                UpdateRowsVisibility();
            }
        }
        // 核心逻辑：根据当前的行数，自动隐藏/显示控件
        private void UpdateRowsVisibility()
        {
            // 遍历 Grid 里的所有控件 (UniversialSplitButton 和 CircleImageButton)
            foreach (UIElement child in MainGrid.Children)
            {
                // 获取当前控件属于第几行 (0代表第一行, 1代表第二行...)
                int rowIndex = System.Windows.Controls.Grid.GetRow(child);

                // 如果控件所在行小于当前允许显示的行数，就显示，否则折叠隐藏
                if (rowIndex < _visibleRowCount)
                {
                    child.Visibility = System.Windows.Visibility.Visible;
                }
                else
                {
                    // Collapsed 会让控件完全消失，且不占位
                    child.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
        }
    }
    public class DuctEditAssembleViewModel : ObserverableObject
    {
        private Document doc;
        private UIDocument uiDoc;
        private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
        public DuctEditAssembleViewModel(UIApplication uiApp)
        {
            uiDoc = uiApp.ActiveUIDocument;
            doc = uiDoc.Document;
        }
        public string TVCommandName1 { get; set; } = "切换风管三通类型";
        public ICommand TVCommand1 => new BaseBindingCommand(TVControl1);
        public void TVControl1(object obj)
        {
            _externalHandler.Run(app =>
            {
                NewTransaction.Execute(doc, "切换风管类型的三通/接头", () =>
                {
                    Reference reference = uiDoc.Selection.PickObject(ObjectType.Element, new filterDuct(), "请选择一个风管");
                    if (reference == null) return;
                    Duct duct = doc.GetElement(reference) as Duct;
                    var ductType = doc.GetElement(duct.GetTypeId()) as DuctType;
                    RoutingPreferenceManager routePrefManager = ductType.RoutingPreferenceManager;
                    string resultMsg = string.Empty;
                    // 直接判断枚举状态并切换
                    if (routePrefManager.PreferredJunctionType == PreferredJunctionType.Tee)
                    {
                        routePrefManager.PreferredJunctionType = PreferredJunctionType.Tap;
                        resultMsg = "接头/直插 (Tap)";
                    }
                    else
                    {
                        routePrefManager.PreferredJunctionType = PreferredJunctionType.Tee;
                        resultMsg = "三通 (Tee)";
                    }
                    // 明确告知最终结果
                    RevitStylePopup.RevitStylePopup.Show("切换完成", $"已将选中风管类型的连接方式切换为：\n\n【 {resultMsg} 】");
                });
            });
        }
        public string TVCommandName2 { get; set; } = "";
        public ICommand TVCommand2 => new BaseBindingCommand(TVControl2);
        public void TVControl2(object obj)
        {

        }
        public string TVCommandName3 { get; set; } = "";
        public ICommand TVCommand3 => new BaseBindingCommand(TVControl3);
        public void TVControl3(object obj)
        {

        }
    }
}
