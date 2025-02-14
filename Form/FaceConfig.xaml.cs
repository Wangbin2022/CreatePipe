using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using CreatePipe.filter;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CreatePipe.Form
{
    /// <summary>
    /// FaceConfig.xaml 的交互逻辑
    /// </summary>
    public partial class FaceConfig : Window
    {
        public FaceConfig(List<WallType> wallTypes)
        {
            InitializeComponent();
            this.DataContext = new ViewModel2(wallTypes);
            // 在窗口加载完成后设置默认选中项
            this.Loaded += (s, e) =>
            {
                if (wallTypes.Count > 0)
                {
                    cbWallType.SelectedIndex = 0;
                }
            };
        }
    }
    public class ViewModel2
    {
        public WallType SelectedWallType { get; set; }
        public ObservableCollection<WallType> WallTypes { get; } = new ObservableCollection<WallType>();
        public ICommand CreateFaceCommand => new BaseBindingCommand(OnCreateFace);
        public ViewModel2(List<WallType> wallTypes)
        {
            wallTypes.ForEach(x => WallTypes.Add(x));
        }
        private void OnCreateFace(object parameter)
        {
            // 这里实现外部事件
            // 这里实现按钮点击的逻辑
            XmlDoc.Instance.Task.Run(app =>
            {
                UIDocument UIDoc = app.ActiveUIDocument;
                var faceReference = UIDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Face, new filterWallClass(), "拾取要复制的墙面");
                var wallofFace = UIDoc.Document.GetElement(faceReference) as Wall;
                var face = wallofFace.GetGeometryObjectFromReference(faceReference) as Face;
                Transaction ts = new Transaction(UIDoc.Document, "创建面生面");
                ts.Start();
                CreateFace(UIDoc.Document, face, wallofFace, SelectedWallType);
                ts.Commit();
                //TaskDialog.Show("tt", "OK");
            });
        }
        private void CreateFace(Document doc, Face face, Wall wallofFace, WallType selectedWallType)
        {
            var profile = new List<Curve>();
            var openingArrays = new List<CurveArray>();
            Double width = selectedWallType.Width;
            ExtractFaceOutline(face, width, ref profile, ref openingArrays);//提取轮廓线
            var wall = Wall.Create(doc, profile, selectedWallType.Id, wallofFace.LevelId, false);
            wall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET).Set(0);//设置底部偏移
            foreach (var item in openingArrays)
            {
                doc.Create.NewOpening(wall, item.get_Item(0).GetEndPoint(0), item.get_Item(1).GetEndPoint(1));
            }
        }
        private void ExtractFaceOutline(Face face, double width, ref List<Curve> profile, ref List<CurveArray> openingArrays) //提取截面，Curve是墙体，CurveArray是墙体上的洞口
        {
            var curveLoops = face.GetEdgesAsCurveLoops();
            XYZ normal = (face as PlanarFace)?.FaceNormal;
            if (normal == null) throw new ArgumentException("非平面不可用");
            Autodesk.Revit.DB.Transform translation = Autodesk.Revit.DB.Transform.CreateTranslation(normal * width / 2);
            int i = 0;
            foreach (var curveLoop in curveLoops.OrderByDescending(x => x.GetExactLength()))
            {
                curveLoop.Transform(translation);
                var array = new CurveArray();
                foreach (var curve in curveLoop)
                {
                    if (i == 0)
                        profile.Add(curve);
                    else array.Append(curve);
                }
                if (i != 0) openingArrays.Add(array);
                i++;
            }
        }
    }
}
