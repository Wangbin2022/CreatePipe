using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe.OfficalSamples
{
    internal class FindSouthFacingWalls
    {
        /// <summary>朝南允许的角度范围（45度 = π/4弧度）</summary>
        private const double SOUTH_ANGLE_TOLERANCE = Math.PI / 4;
        /// <summary>南方方向向量（默认坐标系：Y轴负方向）</summary>
        private static readonly XYZ DefaultSouthDirection = -XYZ.BasisY;

        Document Document;
        private static readonly AddInId AppId = new AddInId(new Guid("8B29D56B-7B9A-4c79-8A38-B1C13B921877"));
        public FindSouthFacingWalls(ExternalCommandData commandData)
        {
            Initialize(commandData);

            using (var trans = new Transaction(commandData.Application.ActiveUIDocument.Document, "查找朝南外墙（无项目北向）"))
            {
                trans.Start();
                Execute(false);  // 不使用项目北向
                //CloseDebugFile();
                trans.Commit();
            }
        }
        /// <summary>
        /// 初始化命令数据
        /// </summary>
        protected void Initialize(ExternalCommandData commandData)
        {
            var Application = commandData.Application.Application;
            Document = commandData.Application.ActiveUIDocument.Document;
        }
        /// <summary>
        /// 执行查找朝南外墙命令
        /// </summary>
        /// <param name="useProjectLocationNorth">是否使用项目位置的北向定义</param>
        protected void Execute(bool useProjectLocationNorth)
        {
            var uiDoc = new UIDocument(Document);

            // 获取当前选中的元素（用于保留已有选择）
            var selectedElements = GetCurrentSelection(uiDoc);

            // 收集所有外墙
            var exteriorWalls = CollectExteriorWalls().ToList();

            // 筛选朝南的外墙
            var southFacingWalls = FindSouthFacingWalls2(exteriorWalls, useProjectLocationNorth);

            // 更新选择集
            UpdateSelection(uiDoc, selectedElements, southFacingWalls);

            // 显示结果
            ShowResult(southFacingWalls.Count(), exteriorWalls.Count);
        }
        /// <summary>
        /// 获取当前选择集中的元素
        /// </summary>
        private static HashSet<ElementId> GetCurrentSelection(UIDocument uiDoc) =>
            uiDoc.Selection.GetElementIds().ToHashSet();

        /// <summary>
        /// 收集所有外墙
        /// </summary>
        protected IEnumerable<Wall> CollectExteriorWalls()
        {
            var collector = new FilteredElementCollector(Document);

            return collector
                .OfClass(typeof(Wall))
                .Cast<Wall>()
                .Where(wall => IsExteriorWall(wall));
        }

        /// <summary>
        /// 判断是否为外墙（C#7.0模式匹配）
        /// </summary>
        protected bool IsExteriorWall(Element element)
        {
            if (!(element is Wall wall)) return false;

            var wallType = Document.GetElement(wall.GetTypeId()) as ElementType;
            if (wallType == null) return false;

            var functionParam = wallType.get_Parameter(BuiltInParameter.FUNCTION_PARAM);
            if (functionParam == null) return false;

            var wallFunction = (WallFunction)functionParam.AsInteger();
            return wallFunction == WallFunction.Exterior;
        }
        /// <summary>
        /// 查找所有朝南的外墙
        /// </summary>
        private IEnumerable<Wall> FindSouthFacingWalls2(IEnumerable<Wall> walls, bool useProjectLocationNorth)
        {
            foreach (var wall in walls)
            {
                var exteriorDirection = GetExteriorWallDirection(wall);

                if (useProjectLocationNorth)
                    exteriorDirection = TransformByProjectLocation(exteriorDirection);

                if (IsSouthFacing(exteriorDirection))
                    yield return wall;
            }
        }

        /// <summary>
        /// 获取外墙的外法线方向
        /// </summary>
        protected XYZ GetExteriorWallDirection(Wall wall)
        {
            if (!(wall.Location is LocationCurve locationCurve))
                return XYZ.BasisZ;

            var curve = locationCurve.Curve;

            // 获取墙的切线方向
            XYZ tangent;
            if (curve is Line)  // C# 7.0+ 支持类型模式匹配（无需捕获变量）
            {
                tangent = curve.ComputeDerivatives(0, true).BasisX.Normalize();
            }
            else
            {
                tangent = (curve.GetEndPoint(1) - curve.GetEndPoint(0)).Normalize();
            }

            // 计算法线方向（Z轴与切线的叉积）
            var exteriorDirection = XYZ.BasisZ.CrossProduct(tangent);

            // 如果墙被翻转，反转法线方向
            if (wall.Flipped)
                exteriorDirection = -exteriorDirection;

            return exteriorDirection;
        }

        /// <summary>
        /// 判断方向是否朝南
        /// </summary>
        /// <param name="direction">待判断的方向向量（需归一化）</param>
        /// <returns>如果在朝南角度范围内则返回true</returns>
        protected bool IsSouthFacing(XYZ direction)
        {
            var angleToSouth = direction.AngleTo(DefaultSouthDirection);
            return Math.Abs(angleToSouth) < SOUTH_ANGLE_TOLERANCE;
        }

        /// <summary>
        /// 根据项目位置变换方向向量
        /// </summary>
        protected XYZ TransformByProjectLocation(XYZ direction)
        {
            var position = Document.ActiveProjectLocation.GetProjectPosition(XYZ.Zero);

            // 创建绕Z轴的旋转变换
            var rotationTransform = Transform.CreateRotation(XYZ.BasisZ, position.Angle);

            // 旋转方向向量
            return rotationTransform.OfVector(direction);
        }
        /// <summary>
        /// 更新选择集
        /// </summary>
        private static void UpdateSelection(UIDocument uiDoc, HashSet<ElementId> existingSelection, IEnumerable<Wall> southFacingWalls)
        {
            // 合并现有选择和朝南外墙
            var newSelection = new HashSet<ElementId>(existingSelection);

            foreach (var wall in southFacingWalls)
            {
                newSelection.Add(wall.Id);
            }

            uiDoc.Selection.SetElementIds(newSelection.ToList());
        }

        /// <summary>
        /// 显示结果
        /// </summary>
        private static void ShowResult(int southFacingCount, int totalWallCount)
        {
            var message = $"查找完成！\n" +
                         $"├─ 外墙总数: {totalWallCount}\n" +
                         $"├─ 朝南外墙数: {southFacingCount}\n" +
                         $"└─ 已添加到当前选择集";

            TaskDialog.Show("查找朝南外墙", message);
        }
    }
    /// <summary>
    /// 查找朝南外墙基类
    /// 提供公共属性和方法
    /// </summary>
    public abstract class FindSouthFacingBase
    {
        #region 属性
        protected Autodesk.Revit.ApplicationServices.Application Application { get; set; }
        protected Document Document { get; set; }
        #endregion

        #region 调试辅助
        private TextWriter _debugWriter;

        /// <summary>
        /// 写入调试信息
        /// </summary>
        protected void WriteDebug(string label, Curve curve)
        {
            //_debugWriter ??= new StreamWriter(@"C:\Directions.txt");
            //var start = curve.GetEndPoint(0);
            //var end = curve.GetEndPoint(1);
            //_debugWriter.WriteLine($"{label} {PointToString(start)} {PointToString(end)}");
        }

        /// <summary>
        /// 将点转换为字符串（C#7.0字符串插值）
        /// </summary>
        private static string PointToString(XYZ point) =>
            $"( {point.X:F3}, {point.Y:F3}, {point.Z:F3} )";

        /// <summary>
        /// 关闭调试文件
        /// </summary>
        protected void CloseDebugFile() => _debugWriter?.Close();
        #endregion


    }
}
