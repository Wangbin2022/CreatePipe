using Autodesk.Revit.Creation;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// 批量创建元素核心类
    /// 演示各种 Revit 批量创建 API 的使用方法
    /// </summary>
    public class ElementsBatchCreator
    {
        private readonly Autodesk.Revit.DB.Document _document;
        private Level _firstLevel;
        private ViewPlan _areaPlanView;

        // 创建配置参数
        private readonly CreationConfig _config = new CreationConfig();

        public ElementsBatchCreator(ExternalCommandData commandData)
        {
            _document = commandData.Application.ActiveUIDocument.Document;
        }

        /// <summary>
        /// 创建所有预定义元素
        /// </summary>
        public bool CreateAllElements()
        {
            // 使用单个事务完成所有创建操作，提高性能
            using (var transaction = new Transaction(_document, "批量创建元素"))
            {
                transaction.Start();

                try
                {
                    // 1. 准备基础数据（获取标高等公共信息）
                    if (!PrepareBaseData())
                    {
                        TaskDialog.Show("错误", "准备基础数据失败，请确保存在'Level 1'标高。");
                        return false;
                    }

                    // 2. 创建基础区域边界（用于后续区域和房间创建）
                    CreateAreaBoundaries();

                    // 3. 创建房间围墙
                    CreateRoomEnclosureWalls();

                    // 4. 批量创建各类元素
                    var results = new Dictionary<string, bool>
                    {
                        ["区域"] = CreateAreas(),
                        ["结构柱"] = CreateColumns(),
                        ["房间"] = CreateRooms(),
                        ["文字注释"] = CreateTextNotes(),
                        ["墙体"] = CreateWalls()
                    };

                    // 5. 执行元素自动连接和刷新
                    _document.AutoJoinElements();
                    _document.Regenerate();

                    // 6. 显示创建结果
                    ShowCreationResults(results);

                    transaction.Commit();

                    // 检查是否有失败的创建操作
                    return results.Values.All(success => success);
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("创建失败", ex.Message);
                    transaction.RollBack();
                    return false;
                }
            }
        }

        /// <summary>
        /// 准备基础数据：获取标高和视图
        /// </summary>
        private bool PrepareBaseData()
        {
            // 获取 Level 1 标高
            _firstLevel = new FilteredElementCollector(_document)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .FirstOrDefault(l => l.Name == "Level 1");

            if (_firstLevel == null) return false;

            // 获取或创建区域平面视图
            var areaScheme = new FilteredElementCollector(_document)
                .OfClass(typeof(AreaScheme))
                .Cast<AreaScheme>()
                .FirstOrDefault(a => a.Name == "Rentable");

            if (areaScheme != null)
            {
                try
                {
                    _areaPlanView = ViewPlan.CreateAreaPlan(_document, areaScheme.Id, _firstLevel.Id);
                    _areaPlanView.Name = "Level 1";
                }
                catch
                {
                    // 区域平面视图可能已存在
                }
            }

            return true;
        }

        /// <summary>
        /// 创建区域边界线
        /// 用于定义区域的轮廓
        /// </summary>
        private void CreateAreaBoundaries()
        {
            if (_areaPlanView == null) return;

            var curves = new List<Curve>();
            var origin = XYZ.Zero;
            var normal = XYZ.BasisZ;

            // 创建水平边界线
            curves.Add(Line.CreateBound(new XYZ(-4, 95, 0), new XYZ(-106, 95, 0)));
            curves.Add(Line.CreateBound(new XYZ(-4, 105, 0), new XYZ(-106, 105, 0)));

            // 创建垂直边界线（11个隔间）
            for (int i = 0; i <= 10; i++)
            {
                var x = -5 - i * 10;
                curves.Add(Line.CreateBound(new XYZ(x, 94, 0), new XYZ(x, 106, 0)));
            }

            // 创建草图平面并添加边界线
            var plane = Plane.CreateByNormalAndOrigin(normal, origin);
            var sketchPlane = SketchPlane.Create(_document, plane);

            foreach (var curve in curves)
            {
                _document.Create.NewAreaBoundaryLine(sketchPlane, curve, _areaPlanView);
            }
        }

        /// <summary>
        /// 创建房间围墙
        /// 定义房间的封闭空间
        /// </summary>
        private void CreateRoomEnclosureWalls()
        {
            // 创建上下水平墙
            var topWall = Line.CreateBound(new XYZ(5, -5, 0), new XYZ(55, -5, 0));
            var bottomWall = Line.CreateBound(new XYZ(5, 5, 0), new XYZ(55, 5, 0));

            Wall.Create(_document, topWall, _firstLevel.Id, true);
            Wall.Create(_document, bottomWall, _firstLevel.Id, true);

            // 创建垂直分隔墙（6个房间）
            for (int i = 0; i <= 5; i++)
            {
                var x = 5 + i * 10;
                var verticalWall = Line.CreateBound(new XYZ(x, -5, 0), new XYZ(x, 5, 0));
                Wall.Create(_document, verticalWall, _firstLevel.Id, true);
            }
        }

        /// <summary>
        /// 批量创建区域
        /// 演示 NewAreas 批量方法的使用
        /// </summary>
        private bool CreateAreas()
        {
            if (_areaPlanView == null) return false;

            var areaCreationData = new List<AreaCreationData>();

            // 创建10个区域的数据
            for (int i = 1; i <= 10; i++)
            {
                var point = new UV(i * -10, 100);
                var areaData = new AreaCreationData(_areaPlanView, point)
                {
                    TagPoint = point
                };
                areaCreationData.Add(areaData);
            }

            if (areaCreationData.Any())
            {
                _document.Create.NewAreas(areaCreationData);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 批量创建结构柱
        /// 演示 NewFamilyInstance 批量方法的使用
        /// </summary>
        private bool CreateColumns()
        {
            // 获取第一个结构柱族类型
            var columnSymbol = new FilteredElementCollector(_document)
                .OfCategory(BuiltInCategory.OST_StructuralColumns)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .FirstOrDefault();

            if (columnSymbol == null) return false;

            // 激活族类型
            if (!columnSymbol.IsActive)
                columnSymbol.Activate();

            // 在10个位置创建结构柱
            for (int i = 1; i <= 10; i++)
            {
                var location = new XYZ(i * 10, 100, 0);
                _document.Create.NewFamilyInstance(location, columnSymbol,
                    _firstLevel, StructuralType.Column);
            }

            return true;
        }

        /// <summary>
        /// 批量创建房间
        /// 演示 NewRoom 批量方法的使用
        /// </summary>
        private bool CreateRooms()
        {
            // 获取第一个阶段（Phase）
            var phase = new FilteredElementCollector(_document)
                .OfClass(typeof(Phase))
                .Cast<Phase>()
                .FirstOrDefault();

            if (phase == null) return false;

            // 创建5个房间
            for (int i = 1; i <= 5; i++)
            {
                var point = new UV(i * 10, 0);
                _document.Create.NewRoom(_firstLevel, point);
            }

            return true;
        }

        /// <summary>
        /// 批量创建文字注释
        /// 演示 TextNote.Create 批量方法的使用
        /// </summary>
        private bool CreateTextNotes()
        {
            // 查找 Level 1 楼层平面视图
            var view = new FilteredElementCollector(_document)
                .OfClass(typeof(ViewPlan))
                .Cast<ViewPlan>()
                .FirstOrDefault(v => !v.IsTemplate
                    && v.Name == "Level 1"
                    && v.ViewType == ViewType.FloorPlan);

            if (view == null) return false;

            // 获取默认文字注释类型
            var textNoteTypeId = _document.GetDefaultElementTypeId(ElementTypeGroup.TextNoteType);

            // 创建5个文字注释
            for (int i = 1; i <= 5; i++)
            {
                var origin = new XYZ(i * -20, -100, 0);
                var textNote = TextNote.Create(_document, view.Id, origin,
                    $"文字注释 {i}", textNoteTypeId);

                if (textNote == null) return false;
            }

            return true;
        }

        /// <summary>
        /// 批量创建墙体
        /// 演示多种墙体形状的创建（直墙、弧形墙）
        /// </summary>
        private bool CreateWalls()
        {
            // 创建带弧形顶的墙体（门头造型）
            for (int i = 1; i <= 10; i++)
            {
                var curves = CreateArchedWallProfile(i);
                Wall.Create(_document, curves, true);
            }

            // 创建普通直墙
            for (int i = 1; i <= 10; i++)
            {
                var start = new XYZ(i * 10, -110, 0);
                var end = new XYZ(i * 10, -120, 0);
                var wallLine = Line.CreateBound(start, end);
                Wall.Create(_document, wallLine, _firstLevel.Id, true);
            }

            return true;
        }

        /// <summary>
        /// 创建弧形墙轮廓
        /// 用于创建带拱形门头的墙体
        /// </summary>
        private static IList<Curve> CreateArchedWallProfile(int index)
        {
            var curves = new List<Curve>();
            var x = index * 10;

            // 墙体的三个直线边
            var topLeft = new XYZ(x, -80, 15);
            var bottomLeft = new XYZ(x, -80, 0);
            var bottomRight = new XYZ(x, -90, 0);
            var topRight = new XYZ(x, -90, 15);

            curves.Add(Line.CreateBound(topLeft, bottomLeft));
            curves.Add(Line.CreateBound(bottomLeft, bottomRight));
            curves.Add(Line.CreateBound(bottomRight, topRight));

            // 弧形顶部
            var arcPoint = new XYZ(x, -85, 20);
            curves.Add(Arc.Create(topRight, topLeft, arcPoint));

            return curves;
        }

        /// <summary>
        /// 显示创建结果统计
        /// </summary>
        private static void ShowCreationResults(Dictionary<string, bool> results)
        {
            var successItems = results.Where(r => r.Value).Select(r => r.Key);
            var failedItems = results.Where(r => !r.Value).Select(r => r.Key);

            var message = $"批量创建完成\n成功：{string.Join(", ", successItems)}";

            if (failedItems.Any())
            {
                message += $"\n失败：{string.Join(", ", failedItems)}";
            }

            TaskDialog.Show("创建结果", message);
        }
    }

    /// <summary>
    /// 创建配置类
    /// 集中管理所有创建参数，便于调整
    /// </summary>
    public class CreationConfig
    {
        // 区域配置
        public int AreaCount { get; set; } = 10;
        public double AreaStartX { get; set; } = -10;
        public double AreaStepX { get; set; } = -10;
        public double AreaY { get; set; } = 100;

        // 柱配置
        public int ColumnCount { get; set; } = 10;
        public double ColumnStartX { get; set; } = 10;
        public double ColumnStepX { get; set; } = 10;
        public double ColumnY { get; set; } = 100;

        // 房间配置
        public int RoomCount { get; set; } = 5;
        public double RoomStartX { get; set; } = 10;
        public double RoomStepX { get; set; } = 10;

        // 文字注释配置
        public int TextNoteCount { get; set; } = 5;
        public double TextNoteStartX { get; set; } = -20;
        public double TextNoteStepX { get; set; } = -20;
        public double TextNoteY { get; set; } = -100;

        // 墙体配置
        public int WallCount { get; set; } = 10;
        public double WallStartX { get; set; } = 10;
        public double WallStepX { get; set; } = 10;
    }
    /// <summary>
    /// 扩展方法：提供更便捷的元素收集
    /// </summary>
    public static partial class DocumentExtensions
    {
        /// <summary>
        /// 获取指定类型的所有元素
        /// </summary>
        public static IEnumerable<T> GetElements<T>(this Autodesk.Revit.DB.Document doc) where T : Element
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(T))
                .Cast<T>();
        }

        /// <summary>
        /// 获取指定类别的所有族符号
        /// </summary>
        public static IEnumerable<FamilySymbol> GetFamilySymbols(this Autodesk.Revit.DB.Document doc, BuiltInCategory category)
        {
            return new FilteredElementCollector(doc)
                .OfCategory(category)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>();
        }
    }
}
