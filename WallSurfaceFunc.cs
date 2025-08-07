using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.filter;
using CreatePipe.Form;
using CreatePipe.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CreatePipe
{
    [Transaction(TransactionMode.Manual)]
    public class WallSurfaceFunc : IExternalCommand
    {
        private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
        public WallType SelectedWallType { get; set; }
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
            UIApplication uiApp = commandData.Application;

            var wallTypes = from element in new FilteredElementCollector(doc).OfClass(typeof(WallType))
                            let type = element as WallType
                            select type;
            List<string> wallNames = new List<string>();
            foreach (var item in wallTypes)
            {
                wallNames.Add(item.Name);
            }
            UniversalComboBoxSelection subView = null;
            // 定义回调方法
            Action<string> onSelected = selectedName =>
            {
                //// 非模态窗口，在这里处理用户选择的结果
                //TaskDialog.Show("选择结果", $"你选择了：{selectedName}");
                foreach (var item in wallTypes)
                {
                    if (item.Name == selectedName)
                    {
                        SelectedWallType = item;
                    }
                }
                _externalHandler.Run(app =>
                {
                    UIDocument UIDoc = app.ActiveUIDocument;
                    var faceReference = UIDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Face,
                    new filterWallClass(), "拾取要复制的墙面");
                    var wallofFace = UIDoc.Document.GetElement(faceReference) as Wall;
                    var face = wallofFace.GetGeometryObjectFromReference(faceReference) as Face;
                    UIDoc.Document.NewTransaction(() =>
                    {
                        var profile = new List<Curve>();
                        var openingArrays = new List<CurveArray>();
                        Double width = SelectedWallType.Width;
                        ExtractFaceOutline(face, width, ref profile, ref openingArrays);//提取轮廓线
                        var wall = Wall.Create(UIDoc.Document, profile, SelectedWallType.Id, wallofFace.LevelId, false);
                        wall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET).Set(0);//设置底部偏移
                        foreach (var item2 in openingArrays)
                        {
                            UIDoc.Document.Create.NewOpening(wall, item2.get_Item(0).GetEndPoint(0), item2.get_Item(1).GetEndPoint(1));
                        }
                    }, "创建面生面");
                    subView.ViewModel.SetCommandCompleted();
                });
            };
            subView = new UniversalComboBoxSelection(wallNames, $"提示：选择面层添加的新墙体材质", onSelected);
            subView.IsModal = false;
            subView.Show();

            ////模态窗口处理返回值bak
            //if (subView.ShowDialog() != true || !(subView.DataContext is ComboboxStringViewModel vm) || string.IsNullOrWhiteSpace(vm.SelectName))
            //{
            //    return Result.Failed;
            //}
            //try
            //{
            //    TaskDialog.Show("tt", vm.SelectName);
            //}
            //catch (Exception ex)
            //{
            //    TaskDialog.Show("tt", $"发生错误: {ex.Message}");
            //}

            return Result.Succeeded;
        }
        //提取截面，Curve是墙体，CurveArray是墙体上的洞口
        private void ExtractFaceOutline(Face face, double width, ref List<Curve> profile, ref List<CurveArray> openingArrays)
        {
            IList<CurveLoop> curveLoops = face.GetEdgesAsCurveLoops();
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
