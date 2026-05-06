using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe.OfficalSamples
{
    internal class ShowElementData
    {
        /// <summary>
        /// 获取选中的元素列表 - 使用LINQ简化
        /// </summary>
        private static List<Element> GetSelectedElements(Autodesk.Revit.DB.Document doc, ICollection<ElementId> selectedIds) =>
            selectedIds.Select(doc.GetElement).Where(e => e != null).ToList();

        /// <summary>
        /// 显示元素类型
        /// </summary>
        private static void ShowElementType(Element element) =>
            ShowMessage($"元素类型: {element.GetType()}");

        /// <summary>
        /// 显示元素位置信息
        /// </summary>
        private static void ShowElementLocation(Element element)
        {
            switch (element.Location)
            {
                case LocationPoint lp:
                    ShowMessage($"位置 - 点: ({lp.Point.X:F3}, {lp.Point.Y:F3}, {lp.Point.Z:F3})");
                    break;

                case LocationCurve lc when lc.Curve is Line line:
                    ShowMessage($"位置 - 直线: ({line.GetEndPoint(0).X:F3}) → ({line.GetEndPoint(1).X:F3})");
                    break;

                case LocationCurve lc:
                    ShowMessage($"位置 - 曲线: {lc.Curve.GetType().Name}");
                    break;

                default:
                    ShowMessage("位置: 无位置信息");
                    break;
            }
        }

        /// <summary>
        /// 显示元素几何信息
        /// </summary>
        private static void ShowElementGeometry(Autodesk.Revit.ApplicationServices.Application app, Element element)
        {
            var options = new Options
            {
                ComputeReferences = true,
                DetailLevel = ViewDetailLevel.Medium
            };

            var geometry = element.get_Geometry(options);
            if (geometry == null)
            {
                ShowMessage("几何体: 无");
                return;
            }

            ShowMessage("几何体信息:");
            TraverseGeometry(app, geometry, 1);
        }

        /// <summary>
        /// 递归遍历几何体 - 使用模式匹配
        /// </summary>
        private static void TraverseGeometry(Autodesk.Revit.ApplicationServices.Application app, GeometryElement geometry, int indentLevel)
        {
            var indent = new string(' ', indentLevel * 2);

            foreach (var geomObj in geometry)
            {
                switch (geomObj)
                {
                    case GeometryInstance instance:
                        ShowMessage($"{indent}几何实例 - 符号几何:");
                        TraverseGeometry(app, instance.SymbolGeometry, indentLevel + 1);
                        break;

                    case Solid solid when solid.Volume > 0:
                        ShowMessage($"{indent}实体 - 体积: {solid.Volume:F3}, 面数: {solid.Faces?.Size ?? 0}");
                        break;

                    case Solid _:
                        ShowMessage($"{indent}实体 - 零体积");
                        break;

                    case Autodesk.Revit.DB.Face face:
                        ShowMessage($"{indent}面 - 面积: {face.Area:F3}");
                        break;

                    case Edge edge:
                        ShowMessage($"{indent}边 - 长度: {edge.ApproximateLength:F3}");
                        break;

                    case Curve curve:
                        ShowMessage($"{indent}曲线 - 长度: {curve.Length:F3}");
                        break;

                    case Autodesk.Revit.DB.Mesh mesh:
                        ShowMessage($"{indent}网格 - 顶点数: {mesh.NumberOfNormals}, 三角形数: {mesh.NumTriangles}");
                        break;

                    case Point point:
                        ShowMessage($"{indent}点 - 坐标: ({point.Coord.X:F3}, {point.Coord.Y:F3}, {point.Coord.Z:F3})");
                        break;
                }
            }
        }

        /// <summary>
        /// 显示元素参数信息 - 使用字符串插值和LINQ
        /// </summary>
        private static void ShowElementParameters(Element element)
        {
            var paramList = new List<string> { "参数信息:" };

            foreach (Parameter param in element.Parameters)
            {
                if (param == null || param.Definition == null) continue;

                var value = GetParameterValue(param);
                paramList.Add($"  {param.Definition.Name} [{param.StorageType}] = {value}");
            }

            ShowMessage(string.Join(Environment.NewLine, paramList));
        }

        /// <summary>
        /// 获取参数值 - 使用switch表达式(C# 8.0)
        /// </summary>
        private static string GetParameterValue(Parameter param)
        {
            switch (param.StorageType)
            {
                case StorageType.String:
                    return param.AsString() ?? "(空)";
                case StorageType.Integer:
                    return param.AsInteger().ToString();
                case StorageType.Double:
                    return param.AsValueString() ?? param.AsDouble().ToString("F3");
                case StorageType.ElementId:
                    return GetElementIdValue(param.AsElementId());
                default:
                    return "(未知类型)";
            }
        }

        /// <summary>
        /// 获取ElementId对应的值
        /// </summary>
        private static string GetElementIdValue(ElementId id) =>
            id == ElementId.InvalidElementId ? "(无)" : $"Element {id.IntegerValue}";

        /// <summary>
        /// 显示分隔线
        /// </summary>
        private static void ShowSeparator() =>
            ShowMessage(new string('=', 50));

        /// <summary>
        /// 显示消息 - 使用Debug输出（可改为TaskDialog）
        /// </summary>
        //private static void ShowMessage(string msg) => Debug.WriteLine(msg);
        private static void ShowMessage(string msg) => TaskDialog.Show("tt", msg);
        public ShowElementData()
        {
            
        }
    }
}
