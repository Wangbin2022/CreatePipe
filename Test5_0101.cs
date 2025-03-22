using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;


namespace CreatePipe
{
    [Transaction(TransactionMode.Manual)]
    public class Test5_0101 : IExternalCommand
    {

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
            UIApplication uiApp = commandData.Application;
            XmlDoc.Instance.UIDoc = uiDoc;
            XmlDoc.Instance.Task = new RevitTask();

            //FilteredElementCollector collector = new FilteredElementCollector(doc, uiDoc.ActiveView.Id).OfClass(typeof(FamilyInstance));
            //List<FamilyInstance> parkingSpaces = collector.Cast<FamilyInstance>().Where(fi => fi.Symbol.Family.Name.Contains("车位")).ToList();
            //TaskDialog.Show("tt", parkingSpaces.Count.ToString());

            //0220 找墙的所有面OK，希望通过这个找出另外搜轮廓办法不要出现多余的参照平面
            //Reference r = uiDoc.Selection.PickObject(ObjectType.Element, new filterWallClass(), "pick wall");
            //Wall wall =doc.GetElement(r.ElementId) as Wall;
            //String faceInfo = "";
            //Autodesk.Revit.DB.Options opt = new Options();
            //Autodesk.Revit.DB.GeometryElement geomElem = wall.get_Geometry(opt);
            //foreach (GeometryObject geomObj in geomElem)
            //{
            //    Solid geomSolid = geomObj as Solid;
            //    if (null != geomSolid)
            //    {
            //        int faces = 0;
            //        double totalArea = 0;
            //        foreach (Face geomFace in geomSolid.Faces)
            //        {
            //            faces++;
            //            faceInfo += "Face " + faces + " area: " + geomFace.Area.ToString() + "\n";
            //            totalArea += geomFace.Area;
            //        }
            //        faceInfo += "Number of faces: " + faces + "\n";
            //        faceInfo += "Total area: " + totalArea.ToString() + "\n";
            //        foreach (Edge geomEdge in geomSolid.Edges)
            //        {
            //            // get wall's geometry edges
            //        }
            //    }
            //}
            //TaskDialog.Show("Revit", faceInfo);

            //////0116 墙面生墙功能代码，模态.OK 0220 继续改解决多余参照平面问题
            //////为了列表完全不得不先导出列表
            //var wallTypes = from element in new FilteredElementCollector(doc).OfClass(typeof(WallType))
            //                let type = element as WallType
            //                select type;
            //var faceConfigWin = new FaceConfig(wallTypes.ToList());//调用xmal生成窗体
            //faceConfigWin.Show();
            //////例程结束
            ////0101 面生面，非模态。OK 
            ////例程结束

            //FamilyManagerView familyManager = new FamilyManagerView(uiApp);
            //familyManager.Show();

            //0211 测试FileModel
            //OpenFileDialog fDialog = new System.Windows.Forms.OpenFileDialog();
            //fDialog.Filter = "RFA 文件 (*.rfa)|*.rfa";
            //if (fDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            //{
            //    FileInfo fileInfo = new FileInfo(fDialog.FileName);
            //    FileSingle fileSingle = new FileSingle(fileInfo);
            //    TaskDialog.Show("tt", fileSingle.DisplaySize);
            //}
            //过程备份
            //RevitContext.Application = uiApp.Application;
            //RevitContext.Document = doc;
            //FileWindow fileWindow = new FileWindow();
            //fileWindow.Show();
            //Save2Csv save2Csv = new Save2Csv();
            //save2Csv.ShowDialog();
            //TestTreeView testTreeView = new TestTreeView();
            //testTreeView.Show();

            ////0210 修改增加防重选和数量提示.OK
            //// 创建 WPF 窗口
            //var countParkingLotForm = new NumberByPickWPF();
            //countParkingLotForm.Show();
            //IList<ElementId> elementIds = new List<ElementId>();
            //bool flag = true;
            //while (flag && countParkingLotForm.DialogResult != true)
            //{
            //    try
            //    {
            //        Reference reference = uiDoc.Selection.PickObject(ObjectType.Element, new ParkingLotFilter(), "单选车位，esc确认");
            //        ElementId elementId = reference.ElementId;
            //        // 检查是否已经选择了该构件
            //        if (!elementIds.Contains(elementId))
            //        {
            //            elementIds.Add(elementId);
            //            countParkingLotForm.SelectedCount++; // 更新选中数量
            //        }
            //        else
            //        {
            //            TaskDialog.Show("提示", "该车位已经选择过，请选择其他车位。");
            //        }
            //    }
            //    catch
            //    {
            //        flag = false;
            //    }
            //}
            //countParkingLotForm.Close();
            //if (elementIds.Count > 0)
            //{
            //    var countParkingLot = new CountParkingLotForm(uiApp, elementIds);
            //    countParkingLot.Show();
            //}
            //else
            //{
            //    TaskDialog.Show("提示", "未选择任何车位。");
            //}
            ////例程结束
            //0208 找未编号清理车位编号不必要，所有车位自动生成有编号
            //0208 全选快速车位编码cmd.OK
            //IList<Reference> parkReference;
            //bool validSelection = false;
            //while (!validSelection)
            //{
            //    parkReference = uiDoc.Selection.PickObjects(Autodesk.Revit.UI.Selection.ObjectType.Element,
            //        new ParkingLotFilter(), "拾取车位");
            //    List<ElementId> ids = new List<ElementId>();
            //    foreach (var item in parkReference)
            //    {
            //        ids.Add(item.ElementId);
            //    }
            //    if (parkReference.Count > 0)
            //    {
            //        validSelection = true;
            //        var countParkingLot = new CountParkingLotForm(uiApp, ids);
            //        countParkingLot.Show();
            //    }
            //    else
            //    {
            //        // 如果没有选择任何元素，弹出提示窗口
            //        TaskDialog.Show("提示", "未找到车位，请重新选择");
            //    }
            //}
            //0208最好加上无选中就拒绝执行
            //例程结束
            //0209 查找车位element
            //找所有车位族。OK
            //FindParkingLotForm findParkingLotForm = new FindParkingLotForm(uiApp);
            //findParkingLotForm.ShowDialog();
            //例程结束
            ////0208 找重叠车位编号.OK
            //IList<Reference> parkReference = uiDoc.Selection.PickObjects(Autodesk.Revit.UI.Selection.ObjectType.Element, new ParkingLotFilter(), "拾取");
            //// 创建一个字典来存储编号和对应的 ElementId
            //var numberToElementIds = new Dictionary<string, List<ElementId>>();
            //foreach (var item in parkReference)
            //{
            //    Element element = uiDoc.Document.GetElement(item.ElementId);
            //    if (element is FamilyInstance familyInstance)
            //    {
            //        // 获取车位编号参数
            //        Parameter parkingNumberParam = familyInstance.LookupParameter("车位编号");
            //        if (parkingNumberParam != null && parkingNumberParam.HasValue)
            //        {
            //            string parkingNumber = parkingNumberParam.AsString();
            //            if (!numberToElementIds.ContainsKey(parkingNumber))
            //            {
            //                numberToElementIds[parkingNumber] = new List<ElementId>();
            //            }
            //            numberToElementIds[parkingNumber].Add(familyInstance.Id);
            //        }
            //    }
            //}
            //// 找出重复的编号
            //var duplicateNumbers = numberToElementIds.Where(kvp => kvp.Value.Count > 1).ToList();
            //if (duplicateNumbers.Any())
            //{
            //    TaskDialog.Show("重复的车位编号", $"检查车位数量{parkReference.Count}，以下车位编号存在重复：");
            //    foreach (var kvp in duplicateNumbers)
            //    {
            //        string messageResult = $"编号: {kvp.Key}, 对应的 ElementId: {string.Join(", ", kvp.Value.Select(id => id.IntegerValue))}";
            //        TaskDialog.Show("重复编号", messageResult);
            //    }
            //}
            //else
            //{
            //    TaskDialog.Show("检查结果", $"检查车位数量{parkReference.Count}，没有发现重复的车位编号。");
            //}
            ////例程结束
            ////0205 过滤车位族属性，统计数量，查找ElemId
            //var ParkReference = uiDoc.Selection.PickObjects(Autodesk.Revit.UI.Selection.ObjectType.Element, new ParkingLotFilter(), "拾取");
            //doc.NewTransaction(() =>
            //{
            //    try
            //    {
            //        //直接以所选集合顺序给号
            //        for (int i = 0; i < ParkReference.Count; i++)
            //        {
            //            Element element = doc.GetElement(ParkReference[i].ElementId);
            //            if (element is FamilyInstance familyInstance)
            //            {
            //                familyInstance.LookupParameter("车位编号").Set((i + 1).ToString());
            //            }
            //        }
            //        //foreach (var item in ParkReference)
            //        //{
            //        //    Element element = doc.GetElement(item.ElementId);
            //        //    if (element is FamilyInstance familyInstance)
            //        //    {
            //        //        familyInstance.LookupParameter("车位编号").Set("1");
            //        //    }
            //        //}
            //    }
            //    catch (Exception)
            //    {
            //        throw;
            //    }
            //}, "写车位信息");
            ////StringBuilder sb = new StringBuilder();
            ////foreach (var item in ParkReference)
            ////{
            ////    Element element = doc.GetElement(item.ElementId);
            ////    sb.AppendLine(element.Id.ToString());
            ////}
            ////TaskDialog.Show("tt", sb.ToString());
            ////TaskDialog.Show("tt", ParkReference.Count().ToString());
            ////例程结束
            ///
            //0203 通过反射返回程序集的地址，应该是dll的
            //string AssemblyLocation = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            //TaskDialog.Show("tt", AssemblyLocation);
            //[Window Title]
            //Add - In Manager(Manual Mode) - tt
            //[Main Instruction]
            //C: \Users\mhzy\AppData\Local\Temp\8837764f - d635 - 4b8e - b585 - c3093f8d8bfe\RevitAddins\CreatePipe - Executing - 20250203_222747_3714
            //[关闭(C)]
            //例程结束
            //// 获取 FamilyManager
            //0114 删除族属性， 区分是否是自带属性，是否只删文字属性？.OK
            //FamilyManager familyManager = doc.FamilyManager;
            //doc.NewTransaction(() =>
            //{
            //    List<FamilyParameter> parameters = familyManager.GetParameters().ToList();
            //    List<ElementId> elementIds = new List<ElementId>();
            //    List<FamilyParameter> newIds = new List<FamilyParameter>();
            //    foreach (FamilyParameter item in parameters)
            //    {
            //        //Definition definition = item.Definition;
            //        //if (definition is InternalDefinition internalDef && internalDef.BuiltInParameter == BuiltInParameter.ALL_MODEL_URL)
            //        //{
            //        //    familyManager.SetParameterLocked(item, true);
            //        //}
            //        Definition definition = item.Definition;
            //        if (definition is InternalDefinition internalDef && internalDef.BuiltInParameter == BuiltInParameter.INVALID)
            //        {
            //            //elementIds.Add(item.Id);
            //            familyManager.RemoveParameter(item);
            //        }
            //        else newIds.Add(item);
            //    }
            //    TaskDialog.Show("tt", newIds.Count().ToString());
            //}, "删除属性");
            //TaskDialog.Show("tt", familyManager.GetParameters().Count().ToString());
            //例程结束
            //0117 新建族属性实验
            //族属性值更改，注意对有公式的参数无效
            //FamilyManager familyManager = doc.FamilyManager;
            //doc.NewTransaction(() =>
            //{
            //    List<FamilyParameter> parameters = familyManager.GetParameters().ToList();
            //    foreach (FamilyParameter item in parameters)
            //    {
            //        if (item.Definition.Name == "套管直径")
            //        {
            //            familyManager.Set(item, 300 / 304.8); //参数复制_参数名、值 
            //        }
            //    }
            //}, "更改属性");
            ////ok
            //FamilyManager familyManager = doc.FamilyManager;
            //doc.NewTransaction(() =>
            //{
            //    familyManager.AddParameter("new", BuiltInParameterGroup.PG_ADSK_MODEL_PROPERTIES, ParameterType.Text, true);//新加参数
            //}, "新增属性");
            ////ok
            //BuiltInParameterGroup.PG_GENERAL = 常规
            //BuiltInParameterGroup.PG_IFC = IFC参数
            //BuiltInParameterGroup.PG_TEXT = 文本
            //BuiltInParameterGroup.PG_DATA = 数据
            //BuiltInParameterGroup.PG_ADSK_MODEL_PROPERTIES = 模型属性
            //BuiltInParameterGroup.PG_TITLE = 标题文字
            //ParameterType.Text = 文字
            //ParameterType.YesNo = 是否
            //ParameterType.FamilyType = 族类型
            //ParameterType.Material = 材质
            //例程结束
            //0117放置族事件.OK
            //OpenFileDialog fDialog = new System.Windows.Forms.OpenFileDialog();
            //if (fDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            //{
            //    string name = System.IO.Path.GetFileNameWithoutExtension(fDialog.FileName);
            //    //TaskDialog.Show("tt", name);
            //    //TaskDialog.Show("tt", fDialog.SafeFileName);//得到文件全名无路径
            //    try
            //    {
            //        doc.NewTransaction(() =>
            //        {
            //            doc.LoadFamily(fDialog.FileName);
            //        }, "导入族");
            //        List<Element> families = new FilteredElementCollector(doc).OfClass(typeof(Family)).Where(x => x.Name == name).ToList();
            //        var family = families.Cast<Family>().FirstOrDefault();
            //        ElementId symbolId = family.GetFamilySymbolIds().FirstOrDefault();
            //        FamilySymbol symbol = doc.GetElement(symbolId) as FamilySymbol;
            //        //PromptForFamilyInstancePlacement自带事务，需要放在事务外
            //        uiDoc.PromptForFamilyInstancePlacement(symbol);
            //    }
            //    catch (Exception)
            //    {
            //    }
            //}
            //例程结束
            //0116取新版本族尝试.OK
            ////using (var folderDialog = new FolderBrowserDialog())
            //OpenFileDialog fDialog = new System.Windows.Forms.OpenFileDialog();
            //if (fDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            //{
            //    try
            //    {
            //        var basicFileInfo = BasicFileInfo.Extract(fDialog.FileName);
            //        TaskDialog.Show("tt", basicFileInfo.Format);//
            //    }
            //    catch (Exception ex)
            //    {
            //        //TaskDialog.Show("tt", ex.Message.ToString());
            //        TaskDialog.Show("tt","不支持版本");
            //    }
            //}
            ////例程结束
            ////0114 取得文件基本属性
            //var basicFileInfo = BasicFileInfo.Extract(doc.PathName);
            //TaskDialog.Show("tt", basicFileInfo.GetDocumentVersion().NumberOfSaves.ToString());//保存次数
            //TaskDialog.Show("tt", basicFileInfo.Format);//年度版本号
            ////例程结束
            //0105 列举选择项的内置参数名称三段下划线分割
            //Reference reference = uiDoc.Selection.PickObject(ObjectType.Element, "选择一个");
            //Element element = doc.GetElement(reference.ElementId);
            //var list = GetBuiltInParametersByElement(element);
            //StringBuilder sb = new StringBuilder();
            //foreach (var item in list)
            //{
            //    sb.AppendLine(item.GetName());
            //}
            //TaskDialog.Show("tt", sb.ToString());
            // 创建一个局部变量来存储 Message 的值,没戏无法传递message
            //string localMessage = message;
            //FilterTestNew filterTestNew = new FilterTestNew(doc,ref localMessage);
            //filterTestNew.ShowDialog();
            //////0105 族文件参数访问
            //if (!doc.IsFamilyDocument)
            //{
            //    message = "This command must be run in a family document.";
            //    return Result.Failed;
            //}
            //// 获取所有参数
            //IEnumerable<FamilyParameter> parameters = familyManager.GetParameters();
            //StringBuilder stringBuilder = new StringBuilder();
            //// 遍历参数并输出名称
            //foreach (FamilyParameter param in parameters)
            //{
            //    //string paramName = param.Definition.Name;
            //    stringBuilder.AppendLine(param.Definition.Name);
            //}
            //TaskDialog.Show("Parameter", stringBuilder.ToString() + parameters.Count());
            //例程结束
            //int index = myList.IndexOf("Banana");
            //FilteredElementCollector elems = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Rooms);
            //List<Room> roomQuery = elems.OfType<Room>().ToList();
            //doc.NewTransaction(() =>
            //{
            //    foreach (var room in roomQuery)
            //    {
            //        try
            //        {
            //            string roomName = room.get_Parameter(BuiltInParameter.ROOM_NAME).AsString();
            //            string roomArea = room.get_Parameter(BuiltInParameter.ROOM_AREA).AsValueString();
            //            room.LookupParameter("空间名称").Set(roomName);
            //            room.LookupParameter("空间面积").Set(roomArea);
            //            room.LookupParameter("所属单体").Set("HZQ02");
            //        }
            //        catch (Exception)
            //        {
            //            throw;
            //        }
            //    }
            //}, "写重复信息");
            //TaskDialog.Show("tt", "PASS");
            //FilteredElementCollector elems = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Rooms);
            //List<Room> rooms = elems.OfType<Room>().ToList();
            //HashSet<string> sinRooms = new HashSet<string>();
            //foreach (var item in rooms)
            //{
            //    sinRooms.Add(item.get_Parameter(BuiltInParameter.ROOM_NAME).AsString());
            //    //sinRooms.Add(item.Name);
            //}
            //StringBuilder sb = new StringBuilder();
            //foreach (var item in sinRooms)
            //{
            //    sb.AppendLine(item);
            //}
            //TaskDialog.Show("tt", sb.ToString());
            //TaskDialog.Show("tt", sinRooms.Count().ToString());
            //0103 csv读取
            //var reader = new StreamReader(File.OpenRead(@"D:\\test1.csv"), System.Text.Encoding.Default);
            //List<string> listA = new List<string>();
            //List<string> listB = new List<string>();
            //while (!reader.EndOfStream)
            //{
            //    string line = reader.ReadLine();
            //    string[] values = line.Split(',');

            //    listA.Add(values[0]);
            //    listB.Add(values[1]);
            //}
            //TaskDialog.Show("tt", listA.Count().ToString() +listB.Last());
            //FilteredElementCollector elems = new FilteredElementCollector(doc).OfClass(typeof(ParameterFilterElement));
            //List<ParameterFilterElement> selects = elems.OfType<ParameterFilterElement>().ToList();

            ////自定义list赋值通用方法
            //List<RevitUnit> collects = new List<RevitUnit>();
            //foreach (var item in selects)
            //{
            //    string name = item.Name;
            //    int id =item.Id.IntegerValue;
            //    // 创建一个新的 RevitUnit 对象，并设置其属性
            //    RevitUnit revitUnit = new RevitUnit
            //    {
            //        Name = name,
            //        ID = id
            //    };
            //    collects.Add(revitUnit);
            //}
            //TaskDialog.Show("tt", collects.Count().ToString());

            //0102 选择房间并读取自定义属性
            //Reference reference = uiDoc.Selection.PickObject(ObjectType.Element, new filterRoomClass(),"选择一个房间");
            //List<Room> rooms = new List<Room>();
            ////IList<Reference> rr = uiDoc.Selection.PickObjects(ObjectType.Element, new filterRoomClass()).ToList();//点选多选
            //IList<Element> rr = uiDoc.Selection.PickElementsByRectangle(new filterRoomClass());//框选多个        
            //foreach (var item in rr)
            //{
            //    Room r=doc.GetElement(item.Id) as Room;
            //    rooms.Add(r);
            //}
            //Room room =rooms.Last();
            ////Room room =doc.GetElement(reference) as Room;
            //string roomName = room.get_Parameter(BuiltInParameter.ROOM_NAME).AsString();//取名称
            //string roomArea = room.get_Parameter(BuiltInParameter.ROOM_AREA).AsValueString();//面积
            ////string roomLevel
            //string defiName= room.LookupParameter("空间名称").AsString();
            //TaskDialog.Show("tt", ChineseToSpell.GetChineseSpell(roomName));//取全称首字母大写
            ////doc.NewTransaction(() =>
            ////{
            ////    //将自定义转内置参数
            ////    //room.Name= defiName;
            ////    //将内置参数转自定义
            ////    //room.LookupParameter("空间名称").Set(roomName);
            ////    //直接写入值
            ////    //room.LookupParameter("所属单体").Set("HZQ01");
            ////}, "测试");
            ////TaskDialog.Show("tt", defiName);
            ////TaskDialog.Show("tt", room.Name);//这样会读取名称加编号

            //0101 读取所选的全部属性
            //Reference reference = uiDoc.Selection.PickObject(ObjectType.Element);
            //Element element =doc.GetElement(reference);
            //GetElementParamenterInformation(doc, element);
            //例程结束
            //TaskDialog.Show("tt", "PASS");
            return Result.Succeeded;
        }
        //根据当前日期组合生成新文件名
        public static string GetNewFilePath(string oldFilePath, string oldFileName)
        {
            string newFilePath = oldFilePath + oldFileName.Substring(0, oldFilePath.IndexOf("."))
                + DateTime.UtcNow.ToString("yyyyMMdd") + oldFileName.Substring(oldFileName.IndexOf("."));
            return newFilePath;
        }
        //改本地文件名.输入文本
        public static void Rename_local(string oldStr, string newStr)
        {
            FileInfo fi = new FileInfo(oldStr);
            FileInfo fi_new = new FileInfo(newStr);
            if (fi_new.Exists)
            {
                fi_new.Delete();
            }
            fi.MoveTo(Path.Combine(newStr));
        }
        static List<BuiltInParameter> GetBuiltInParametersByElement(Element element)
        {
            List<BuiltInParameter> bips = new List<BuiltInParameter>();
            foreach (BuiltInParameter bip in BuiltInParameter.GetValues(typeof(BuiltInParameter)))
            {
                Parameter p = element.get_Parameter(bip);
                if (p != null)
                {
                    bips.Add(bip);
                }
            }
            return bips;
        }
        void GetElementParamenterInformation(Document document, Element element)
        {
            String prompt = "Show Parameters in selected Element";
            //string prompt = null;
            StringBuilder st = new StringBuilder();
            foreach (Parameter para in element.Parameters)
            {
                st.AppendLine(GetParameterInformation(para, document));
            }
            //MessageBox.Show(prompt, "Revit", MessageBoxButtons.OK);
            System.Windows.Forms.MessageBox.Show(st.ToString(), "Revit", MessageBoxButtons.OK);
        }
        String GetParameterInformation(Parameter para, Document document)
        {
            string defName = para.Definition.Name + "\t";
            switch (para.StorageType)
            {
                case StorageType.Double:
                    defName += ":" + para.AsValueString();
                    break;
                case StorageType.ElementId:
                    ElementId id = para.AsElementId();
                    if (id.IntegerValue > 0)
                    {
                        defName += ":" + document.GetElement(id).Name;
                    }
                    else
                        defName += ":" + id.IntegerValue.ToString();
                    break;
                case StorageType.Integer:
                    if (ParameterType.YesNo == para.Definition.ParameterType)
                    {
                        if (para.AsInteger() == 0)
                        {
                            defName += ":" + "False";
                        }
                        else
                        {
                            defName += ":" + "True";
                        }
                    }
                    else
                    {
                        defName += ":" + para.AsInteger().ToString();
                    }
                    break;

                case StorageType.String:
                    defName += ":" + para.AsString();
                    break;
                default:
                    defName = "Unexpected parameter";
                    break;
            }
            return defName;
        }
    }
    public class RevitUnit
    {
        public int ID { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
    }
    //public class ViewModelTest : ObserverableObject
    //{
    //    private List<TreeNode> treenodes = new List<TreeNode>();
    //    public List<TreeNode> Treenodes
    //    {
    //        get => treenodes;
    //        set => SetProperty(ref treenodes, value);
    //    }
    //    private List<TreeNode> nodes;
    //    public List<TreeNode> Nodes
    //    {
    //        get => nodes;
    //        set => SetProperty(ref nodes, value);
    //    }
    //    private TreeNode currentNode;
    //    public TreeNode CurrentNode
    //    {
    //        get => currentNode;
    //        set => SetProperty(ref currentNode, value);
    //    }
    //    public ICommand SelectCommand { get; set; }
    //    public ViewModelTest(Document document)
    //    {
    //        Nodes = new List<TreeNode>()
    //        {
    //            new TreeNode(){ ParentId=0,NodeID=1,NodeName="书本"},
    //            new TreeNode(){ ParentId=0,NodeID=2,NodeName="课桌"},
    //            new TreeNode(){ ParentId=0,NodeID=3,NodeName="文具"},
    //            new TreeNode(){ ParentId=1,NodeID=4,NodeName="书本名"},
    //            new TreeNode(){ ParentId=1,NodeID=5,NodeName="作者"},
    //            new TreeNode(){ ParentId=2,NodeID=6,NodeName="材质"},
    //            new TreeNode(){ ParentId=3,NodeID=7,NodeName="品牌1"},
    //            new TreeNode(){ ParentId=6,NodeID=8,NodeName="材质1"},
    //            new TreeNode(){ ParentId=6,NodeID=9,NodeName="材质2"},
    //            new TreeNode(){ ParentId=2,NodeID=10,NodeName="编号"},
    //            new TreeNode(){ ParentId=3,NodeID=11,NodeName="品牌2"},
    //        };
    //        treenodes = getChildNode(0, Nodes);
    //        SelectCommand = new RelayCommand<Object>(Select);
    //    }
    //    private List<TreeNode> getChildNode(int parentID, List<TreeNode> nodes)
    //    {
    //        List<TreeNode> mainNodes = nodes.Where(x => x.ParentId == parentID).ToList();
    //        List<TreeNode> otherNodes = nodes.Where(x => x.ParentId != parentID).ToList();
    //        foreach (var node in mainNodes)
    //        {
    //            node.ChildNodes=getChildNode(parentID, node.ChildNodes);
    //        }
    //        return mainNodes;
    //    }
    //    private void Select(object obj)
    //    {
    //        var res = obj as TreeNode;
    //        if (res != null && res.ChildNodes.Count == 0)
    //        {
    //            CurrentNode = res;
    //        }
    //    }
    //}
    //public class TreeNode
    //{
    //    public int NodeID { get; set; }
    //    public int ParentId { get; set; }
    //    public string NodeName { get; set; }
    //    public List<TreeNode> ChildNodes { get; set; }
    //    public TreeNode()
    //    {
    //        ChildNodes = new List<TreeNode>();
    //    }
    //}
}
