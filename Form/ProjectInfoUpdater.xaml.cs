using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using CreatePipe.Utils;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CreatePipe.Form
{
    /// <summary>
    /// ProjectInfoUpdater.xaml 的交互逻辑
    /// </summary>
    public partial class ProjectInfoUpdater : Window
    {
        public ProjectInfoUpdater(UIApplication uiApp)
        {
            InitializeComponent();
            this.DataContext = new ProjectInfoUpdaterViewModel(uiApp.ActiveUIDocument.Document);
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //属性更新代码放到command中，优先处理          
            this.Close();
        }
    }

    public class ProjectInfoUpdaterViewModel
    {
        private readonly Document _doc;
        public ProjectInfoUpdaterViewModel(Document doc)
        {
            _doc = doc;
            try
            {
                if (!Clipboard.ContainsText())
                {
                    TaskDialog.Show("警告", "剪贴板中不包含文本信息。");
                    return;
                }
                string text = Clipboard.GetText().Trim();
                if (string.IsNullOrEmpty(text)) return;
                // 解析 CSV 格式 (允许逗号或中文逗号)
                string[] items = text.Split(new[] { ',', '，' }, StringSplitOptions.None).Select(s => s.Trim()).ToArray();
                if (items.Length < 6)
                {
                    TaskDialog.Show("格式错误", "信息项不足 6 项，请确保格式为：\n委托方,项目名称,建筑名称,编号,日期,阶段");
                    return;
                }
                // 更新 UI 绑定属性
                ProjectClt = items[0];
                ProjectName = items[1];
                BuildingName = items[2];
                ProjectNumber = items[3];
                IssueDate = items[4];
                ProjectStatus = items[5];
                InputRawData = text;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("错误", "读取剪贴板失败: " + ex.Message);
            }
        }
        public string ProjectClt { get; set; }
        public string ProjectName { get; set; }
        public string BuildingName { get; set; }
        public string ProjectNumber { get; set; }
        public string IssueDate { get; set; }
        public string ProjectStatus { get; set; }
        public string InputRawData { get; set; }
        public ICommand SaveToRevitCommand => new BaseBindingCommand(ExecuteSaveToRevit);
        /// <summary>
        /// 将当前属性写入 Revit
        /// </summary>
        private void ExecuteSaveToRevit(Object obj)
        {
            ProjectInfo proj = _doc.ProjectInformation;
            NewTransaction.Execute(_doc, "修改项目信息", () =>
            {
                try
                {
                    SetParamValue(proj, BuiltInParameter.CLIENT_NAME, ProjectClt);
                    SetParamValue(proj, BuiltInParameter.PROJECT_NAME, ProjectName);
                    SetParamValue(proj, BuiltInParameter.PROJECT_BUILDING_NAME, BuildingName);
                    SetParamValue(proj, BuiltInParameter.PROJECT_NUMBER, ProjectNumber);
                    SetParamValue(proj, BuiltInParameter.PROJECT_ISSUE_DATE, IssueDate);
                    SetParamValue(proj, BuiltInParameter.PROJECT_STATUS, ProjectStatus);
                    TaskDialog.Show("成功", "项目信息已成功更新至 Revit。");
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("更新失败", ex.Message);
                }
            });
        }
        private void SetParamValue(Element elem, BuiltInParameter bip, string value)
        {
            Parameter p = elem.get_Parameter(bip);
            if (p != null && !p.IsReadOnly)
            {
                p.Set(value ?? string.Empty);
            }
        }
    }
}
