using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CreatePipe.cmd;
using CreatePipe.models;
using CreatePipe.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CreatePipe.Form
{
    /// <summary>
    /// DuctSystemManagerView.xaml 的交互逻辑
    /// </summary>
    public partial class DuctSystemManagerView : Window
    {
        public Document Doc { get; set; }
        public UIApplication UIApplication { get; set; }
        public DuctSystemManagerView(UIApplication uIApplication)
        {
            Doc = uIApplication.ActiveUIDocument.Document;
            UIApplication = uIApplication;
            InitializeComponent();
            this.DataContext = new DuctSystemManagerViewModel(uIApplication);
        }
        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void Btn_Add_Click(object sender, RoutedEventArgs e)
        {
            DuctSystemManagerViewModel viewModel = new DuctSystemManagerViewModel(UIApplication);
            viewModel.AddElement(Doc);
            this.Close();
        }
    }
    public class DuctSystemManagerViewModel : ObserverableObject
    {
        public Document Doc { get; set; }
        public UIDocument UIDocument { get; set; }
        public ObservableCollection<DuctSystemEntity> DuctSystemEntitys { get; set; } = new ObservableCollection<DuctSystemEntity>();
        public DuctSystemManagerViewModel(UIApplication uIApplication)
        {
            Doc = uIApplication.ActiveUIDocument.Document;
            UIDocument = uIApplication.ActiveUIDocument as UIDocument;
            LoadAndInitializeDuctSystems();
            QueryELements(null);
        }
        private void LoadAndInitializeDuctSystems()
        {
            // --- 核心修改部分：使用 TransactionGroup ---
            TransactionGroup tg = new TransactionGroup(Doc, "初始化风管系统设置");
            try
            {
                tg.Start();
                // 1. 查询所有管道系统类型
                var elements = new FilteredElementCollector(Doc).OfClass(typeof(MechanicalSystemType));
                List<MechanicalSystemType> ductSystemTypes = elements.OfType<MechanicalSystemType>().ToList();
                var defaultMaterialId = new FilteredElementCollector(Doc).OfCategory(BuiltInCategory.OST_Materials).FirstOrDefault().Id;
                // 2. 在一个单独的事务中设置默认颜色
                using (var trans = new Transaction(Doc, "设置默认系统颜色材质"))
                {
                    trans.Start();
                    bool changesMade = false;
                    Random rand = new Random();
                    foreach (var pst in ductSystemTypes)
                    {
                        // 检查颜色是否有效，如果无效，则设置一个随机的默认颜色
                        if (!pst.LineColor.IsValid)
                        {
                            //byte r = (byte)rand.Next(50, 220);  
                            //byte g = (byte)rand.Next(50, 220);
                            //byte b = (byte)rand.Next(50, 220);
                            //pst.LineColor = new Autodesk.Revit.DB.Color(r, g, b);
                            pst.LineColor = new Autodesk.Revit.DB.Color(0, 0, 0);
                            changesMade = true;
                        }
                        if (pst.MaterialId.IntegerValue == -1)
                        {
                            pst.MaterialId = defaultMaterialId;
                            changesMade = true;
                        }
                    }
                    if (changesMade) trans.Commit();
                    else trans.RollBack(); // 如果没有做任何修改，则回滚事务
                }
                // 3. 将处理后的数据加载到ViewModel中
                //DuctSystemEntitys.Clear();
                //var ductSystems = ductSystemTypes.Select(dst => new DuctSystemEntity(dst)).ToList();
                //foreach (var item in ductSystems)
                //{
                //    DuctSystemEntitys.Add(item);
                //}
                // 4. 同化事务组，将所有子事务合并成一个撤销操作
                tg.Assimilate();
            }
            catch (Exception ex)
            {
                // 如果发生任何错误，回滚整个事务组
                tg.RollBack();
                TaskDialog.Show("错误", "初始化风管系统时出错: " + ex.Message);
            }
        }
        public ICommand SelectSystemCommand => new Form.RelayCommand<IEnumerable<object>>(SelectSystems);
        private void SelectSystems(IEnumerable<object> selectedElements)
        {
            if (selectedElements.Count() == 0)
            {
                TaskDialog.Show("tt", "未选择任何系统，请重试");
                return;
            }
            List<DuctSystemEntity> selectedItems = selectedElements.Cast<DuctSystemEntity>().ToList();
            if (selectedElements == null) return;
            List<ElementId> ids = new List<ElementId>();
            foreach (var system in selectedItems)
            {
                foreach (var item in system.selectedElements)
                {
                    if (item is ElementId id)
                    {
                        ids.Add(id);
                    }
                }
            }
            Selection select = UIDocument.Selection;
            select.SetElementIds(ids);
        }
        public ICommand AddInsulationCommand => new Form.RelayCommand<DuctSystemEntity>(AddInsulation);
        public void AddInsulation(DuctSystemEntity ductSystem)
        {
            //DuctInsulationAddView ductInsulationAdd = new DuctInsulationAddView(ductSystem);
            //ductInsulationAdd.ShowDialog();
        }
        public ICommand WindowCommand => new BaseBindingCommand(Window);
        //修改材质直接开系统的吧，怎么接收修改？
        public void Window(object para)
        {
            MainWindow materialManager = new MainWindow(Doc);
            //MaterialManagerView materialManager = new MaterialManagerView(Doc);
            bool? result = materialManager.ShowDialog();
            if (result == true) { QueryELements(null); }
        }
        public void AddElement(Document document)
        {
            //DuctSystemAddView ductSystemAddView = new DuctSystemAddView(document);
            //ductSystemAddView.ShowDialog();
        }
        //检查系统是否为空
        private bool isEmptySystem { get; set; }
        public bool IsEmptySystem(DuctSystemEntity systemType)
        {//RBS_PIPING_SYSTEM_TYPE_PARAM
            ElementParameterFilter filter = new ElementParameterFilter(ParameterFilterRuleFactory.CreateEqualsRule(new ElementId(BuiltInParameter.RBS_DUCT_SYSTEM_TYPE_PARAM), systemType.SystemName, false));
            IList<Element> allpipes = new FilteredElementCollector(systemType.Document)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.OST_PipeCurves)
                .WherePasses(filter)
                .ToElements();
            IList<Element> allfamily = new FilteredElementCollector(systemType.Document)
                 .WhereElementIsNotElementType()
                 .OfClass(typeof(FamilyInstance))
                 .WherePasses(filter)
                 .ToElements();

            int countEntity = allpipes.Count() + allfamily.Count();
            if (countEntity == 0) { return true; }
            else return false;
        }
        //检查系统唯一性
        private bool isLastSystemEntity { get; set; }
        public bool IsLastSystemEntity(DuctSystemEntity systemType)
        {
            FilteredElementCollector elems = new FilteredElementCollector(Doc).OfClass(typeof(MEPSystemType));
            List<MEPSystemType> systemTypes = elems.OfType<MEPSystemType>().ToList();
            int systemCount = 0;
            foreach (MEPSystemType item in systemTypes)
            {
                if (systemType.MEPSystemClassOrigin == item.SystemClassification)
                {
                    systemCount++;
                }
            }
            if (systemCount > 1)
            {
                return false;
            }
            return true;
        }
        public ICommand DeleteELementCommand => new Form.RelayCommand<IEnumerable<object>>(DeleteElements);
        public ICommand DeleteELementCommand2 => new Form.RelayCommand<DuctSystemEntity>(DeleteElement);
        //多选删除
        public void DeleteElements(IEnumerable<object> selectedElements)
        {
            List<DuctSystemEntity> selectedItems = selectedElements.Cast<DuctSystemEntity>().ToList();
            if (selectedElements == null) return;
            foreach (var item in selectedItems)
            {
                DeleteElement(item);
            }
        }
        //单选删除
        public void DeleteElement(DuctSystemEntity ductSystemSingle)
        {
            Document document = ductSystemSingle.Document;
            isLastSystemEntity = IsLastSystemEntity(ductSystemSingle);
            isEmptySystem = IsEmptySystem(ductSystemSingle);
            if (!isLastSystemEntity)
            {
                if (isEmptySystem)
                {
                    document.NewTransaction(() =>
                    {
                        document.Delete(ductSystemSingle.ductSystemSingle.Id);
                        DuctSystemEntitys.Remove(ductSystemSingle);
                    }, "删除系统");
                    OnPropertyChanged(nameof(DuctSystemCount));
                }
                else
                {
                    TaskDialog td = new TaskDialog("tt");
                    td.MainInstruction = "选定的系统类型正在使用，因此不能删除";
                    td.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
                    td.Show();
                }
            }
            else
            {
                TaskDialog td = new TaskDialog("tt");
                td.MainInstruction = "不可删除该类型最后一个系统实例";
                td.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
                td.Show();
            }
        }
        public ICommand SetColorCommand => new Form.RelayCommand<MechanicalSystemType>(SetColor);
        private void SetColor(MechanicalSystemType ductSystemType)
        {
            if (ductSystemType == null) return;
            System.Windows.Forms.ColorDialog dialog = new System.Windows.Forms.ColorDialog();
            dialog.AllowFullOpen = true;
            dialog.FullOpen = true;
            dialog.ShowHelp = true;
            Autodesk.Revit.DB.Color color = ductSystemType.LineColor;
            dialog.Color = System.Drawing.Color.FromArgb(color.Red, color.Green, color.Blue);
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ductSystemType.Document.NewTransaction(() => ductSystemType.LineColor = dialog.Color.ConvertToRevitColor(), "修改线颜色");
                QueryELements(null);
            }
            ;
        }
        public ICommand QueryELementCommand => new BaseBindingCommand(QueryELements);
        public void QueryELements(object parameter)
        {
            DuctSystemEntitys.Clear();
            FilteredElementCollector elements = new FilteredElementCollector(Doc).OfClass(typeof(MechanicalSystemType));
            List<MechanicalSystemType> ductSystemTypes = elements.OfType<MechanicalSystemType>().ToList(); //// 使用 OfType 直接过滤并转换类型转换为 List
            List<DuctSystemEntity> ductSystems = ductSystemTypes
                .Select(ductSystemType => new DuctSystemEntity(ductSystemType))
                .Where(e => string.IsNullOrEmpty(Keyword)
                || e.SystemName.Contains(Keyword)
                || e.Abbreviation.IndexOf(Keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
            foreach (var item in ductSystems)
            {
                DuctSystemEntitys.Add(item);
            }
        }
        public string DuctSystemCount => DuctSystemEntitys.Count.ToString();
        private string _keyword;
        public string Keyword
        {
            get => _keyword; set => _keyword = value;
        }
    }
}
