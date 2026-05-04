using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CreatePipe.OfficalSamples
{
    internal class WallCompoundStructureCommand
    {
        #region 成员变量
        private UIApplication _uiApp;
        private UIDocument _uiDoc;
        // 墙体层厚度常量 (单位: 英尺)
        private const double FINISH_LAYER_THICKNESS = 0.2;      // 饰面层厚度
        private const double SUBSTRATE_LAYER_THICKNESS = 0.1;   // 基层厚度
        private const double STRUCTURE_LAYER_THICKNESS = 0.5;   // 结构层厚度
        private const double MEMBRANE_LAYER_THICKNESS = 0.0;    // 膜层厚度
        // 墙饰条/嵌条偏移常量
        private const double WALL_OFFSET = -0.1;                // 墙体偏移量
        private const double SWEEP_DISTANCE_FACTOR = 0.25;      // 饰条距离系数 (1/4)
        private const double REVEAL_DISTANCE_FACTOR = 0.5;      // 嵌条距离系数 (1/2)
        // 材质常量
        private const string BRICK_MATERIAL_NAME = "Brick, Common";
        private const string BRICK_NEW_MATERIAL_NAME = "Brick, Common_new";
        private const string CONCRETE_MATERIAL_NAME = "Concrete, Lightweight";
        private const string CONCRETE_NEW_MATERIAL_NAME = "Concrete, Lightweight_new";
        private const string PROFILE_NAME = "8\" Wide";
        #endregion
        public WallCompoundStructureCommand(ExternalCommandData commandData)
        {
            //TaskDialog.Show("tt", "PASS");
            // 初始化成员变量
            _uiApp = commandData?.Application;
            _uiDoc = _uiApp?.ActiveUIDocument;
            string message = string.Empty;
            // 使用模式匹配验证
            if (!TryInitialize(commandData, ref message, out var document))
                return;
            // 获取选中的墙体
            var selectedWalls = GetSelectedWalls();
            if (!selectedWalls.Any())
            {
                message = "请至少选择一面墙体";
                return;
            }
            // 使用本地函数处理主逻辑
            ProcessWalls(selectedWalls, ref message);
        }
        /// <summary>
        /// 尝试初始化命令
        /// 使用C# 7.3的out变量和模式匹配
        /// </summary>
        private bool TryInitialize(ExternalCommandData commandData, ref string message, out Document document)
        {
            document = null;

            // 使用模式匹配进行验证
            if (commandData is null)
            {
                message = "命令数据为空";
                return false;
            }

            _uiApp = commandData.Application;
            if (_uiApp is null)
            {
                message = "无法获取Revit应用程序";
                return false;
            }

            _uiDoc = _uiApp.ActiveUIDocument;
            if (_uiDoc is null)
            {
                message = "无法获取活动文档";
                return false;
            }

            document = _uiDoc.Document;
            return true;
        }

        /// <summary>
        /// 获取选中的墙体元素
        /// 使用C# 7.3的LINQ和模式匹配
        /// </summary>
        private IEnumerable<Wall> GetSelectedWalls()
        {
            var selectedIds = _uiDoc.Selection.GetElementIds();

            // 使用C# 7.3的LINQ和模式匹配简化
            return selectedIds
                .Select(id => _uiDoc.Document.GetElement(id))
                .OfType<Wall>()  // 使用OfType替代手动类型检查
                .ToList();
        }

        /// <summary>
        /// 处理选中的墙体
        /// 使用C# 7.3的本地函数
        /// </summary>
        private Result ProcessWalls(IEnumerable<Wall> walls, ref string message)
        {
            var transaction = new Transaction(_uiDoc.Document, "创建墙体复合结构");
            try
            {
                transaction.Start();

                var updatedCount = 0;
                foreach (var wall in walls)
                {
                    if (CreateCompoundStructureForWall(wall))
                    {
                        updatedCount++;
                    }
                }

                transaction.Commit();

                message = $"成功更新 {updatedCount} 面墙体的复合结构";
                return updatedCount > 0 ? Result.Succeeded : Result.Cancelled;
            }
            catch (Exception ex)
            {
                transaction.RollBack();
                message = $"处理失败: {ex.Message}";
                return Result.Failed;
            }
        }
        /// <summary>
        /// 为墙体创建复合结构
        /// 使用C# 7.3的本地函数和元组解构
        /// </summary>
        private bool CreateCompoundStructureForWall(Wall wall)
        {
            // 防御性检查
            if (wall is null) return false;

            var wallType = wall.WallType;
            if (wallType is null) return false;

            // 获取或创建复合结构
            var compoundStructure = wallType.GetCompoundStructure() ?? CreateDefaultCompoundStructure();

            // 创建材质
            var (brickMaterial, concreteMaterial) = CreateSampleMaterials();

            // 创建复合结构层
            var layers = CreateCompoundStructureLayers(brickMaterial, concreteMaterial);
            compoundStructure.SetLayers(layers);

            // 设置结构分析层索引
            compoundStructure.StructuralMaterialIndex = GetStructuralLayerIndex(layers);

            // 设置外壳层和包裹参数
            ConfigureShellAndWrapping(compoundStructure);

            // 添加墙饰条和墙嵌条
            var (sweepPoint, revealPoint) = AddWallSweepsAndReveals(compoundStructure);

            // 应用复合结构到墙体类型
            wallType.SetCompoundStructure(compoundStructure);

            return true;
        }

        /// <summary>
        /// 创建默认复合结构
        /// 使用C# 7.3的表达式体
        /// </summary>
        private CompoundStructure CreateDefaultCompoundStructure() =>
            CompoundStructure.CreateSimpleCompoundStructure(new List<CompoundStructureLayer>());

        /// <summary>
        /// 创建复合结构层
        /// 使用C# 7.3的集合初始化器和元组
        /// </summary>
        private List<CompoundStructureLayer> CreateCompoundStructureLayers(Material brickMaterial, Material concreteMaterial)
        {
            // 定义层参数元组 (厚度, 功能分配, 材质ID)
            var layerDefinitions = new (double thickness, MaterialFunctionAssignment function, ElementId materialId)[]
            {
                (FINISH_LAYER_THICKNESS, MaterialFunctionAssignment.Finish1, brickMaterial.Id),      // 外墙饰面层 - 砖
                (SUBSTRATE_LAYER_THICKNESS, MaterialFunctionAssignment.Substrate, ElementId.InvalidElementId), // 基层
                (STRUCTURE_LAYER_THICKNESS, MaterialFunctionAssignment.Structure, concreteMaterial.Id),       // 结构层 - 混凝土
                (MEMBRANE_LAYER_THICKNESS, MaterialFunctionAssignment.Membrane, ElementId.InvalidElementId),  // 膜层
                (FINISH_LAYER_THICKNESS, MaterialFunctionAssignment.Finish2, concreteMaterial.Id)      // 内墙饰面层 - 混凝土
            };

            // 使用C# 7.3的LINQ创建层列表
            return layerDefinitions
                .Select(def => new CompoundStructureLayer(def.thickness, def.function, def.materialId))
                .ToList();
        }

        /// <summary>
        /// 获取结构层索引
        /// 使用C# 7.3的LINQ和模式匹配
        /// </summary>
        private int GetStructuralLayerIndex(List<CompoundStructureLayer> layers)
        {
            // 查找功能分配为Structure的层索引
            for (int i = 0; i < layers.Count; i++)
            {
                if (layers[i].Function == MaterialFunctionAssignment.Structure)
                    return i;
            }
            return 2; // 默认返回第3层(索引2)作为结构层
        }

        /// <summary>
        /// 配置外壳层和包裹参数
        /// 使用C# 7.3的表达式体
        /// </summary>
        private void ConfigureShellAndWrapping(CompoundStructure compoundStructure)
        {
            // 设置内外侧外壳层数
            compoundStructure.SetNumberOfShellLayers(ShellLayerType.Interior, 2);
            compoundStructure.SetNumberOfShellLayers(ShellLayerType.Exterior, 1);

            // 设置第0层是否参与包裹
            compoundStructure.SetParticipatesInWrapping(0, false);
        }
        /// <summary>
        /// 添加墙饰条和墙嵌条
        /// 返回饰条点和嵌条点（使用元组）
        /// </summary>
        private (UV sweepPoint, UV revealPoint) AddWallSweepsAndReveals(CompoundStructure compoundStructure)
        {
            // 获取第一个面段ID
            var segmentIds = compoundStructure.GetSegmentIds();
            if (!segmentIds.Any())
                return (UV.Zero, UV.Zero);

            var segmentId = segmentIds.First();
            var sweepPoint = UV.Zero;
            var revealPoint = UV.Zero;

            // 遍历相邻区域
            foreach (var regionId in compoundStructure.GetAdjacentRegions(segmentId))
            {
                // 获取面段端点（使用out变量）
                var (endPoint1, endPoint2) = GetSegmentEndPoints(compoundStructure, segmentId, regionId);
                if (endPoint1 is null || endPoint2 is null) continue;

                // 计算分割方向和分割点
                var splitOrientation = GetPerpendicularOrientation(compoundStructure.GetSegmentOrientation(segmentId));
                var splitPoint = (endPoint1 + endPoint2) / 2.0;

                // 分割区域
                var newRegionId = compoundStructure.SplitRegion(splitPoint, splitOrientation);

                // 查找包围区域和被分割的面段
                var (segId1, segId2) = FindEnclosingSegments(compoundStructure, splitPoint, splitOrientation);

                // 计算墙饰条和墙嵌条位置
                sweepPoint = CalculateSweepPoint(compoundStructure, segId1, regionId);
                revealPoint = CalculateRevealPoint(compoundStructure, segId2, regionId);
            }

            // 添加墙饰条和墙嵌条
            AddWallSweep(compoundStructure, sweepPoint, "Sweep");
            AddWallReveal(compoundStructure, revealPoint, "Reveal");

            return (sweepPoint, revealPoint);
        }

        /// <summary>
        /// 获取面段端点（使用元组返回）
        /// </summary>
        private (UV endPoint1, UV endPoint2) GetSegmentEndPoints(CompoundStructure cs, int segmentId, int regionId)
        {
            cs.GetSegmentEndPoints(segmentId, regionId, out var ep1, out var ep2);
            return (ep1, ep2);
        }

        /// <summary>
        /// 获取垂直方向
        /// 使用C# 7.3的switch表达式
        /// </summary>
        private RectangularGridSegmentOrientation GetPerpendicularOrientation(RectangularGridSegmentOrientation orientation)
        {
            switch (orientation)
            {
                case RectangularGridSegmentOrientation.Horizontal:
                    return RectangularGridSegmentOrientation.Vertical;
                case RectangularGridSegmentOrientation.Vertical:
                    return RectangularGridSegmentOrientation.Horizontal;
                default:
                    return orientation;
            }
        }

        /// <summary>
        /// 查找包围区域和被分割的面段（使用元组返回）
        /// </summary>
        private (int segId1, int segId2) FindEnclosingSegments(CompoundStructure cs, UV splitPoint,
            RectangularGridSegmentOrientation orientation)
        {
            cs.FindEnclosingRegionAndSegments(splitPoint, orientation, out var segId1, out var segId2);
            return (segId1, segId2);
        }

        /// <summary>
        /// 计算墙饰条位置
        /// </summary>
        private UV CalculateSweepPoint(CompoundStructure cs, int segmentId, int regionId)
        {
            cs.GetSegmentEndPoints(segmentId, regionId, out var ep1, out var ep2);
            return (ep1 + ep2) * SWEEP_DISTANCE_FACTOR;  // 使用系数计算位置
        }

        /// <summary>
        /// 计算墙嵌条位置
        /// </summary>
        private UV CalculateRevealPoint(CompoundStructure cs, int segmentId, int regionId)
        {
            cs.GetSegmentEndPoints(segmentId, regionId, out var ep1, out var ep2);
            return (ep1 + ep2) * REVEAL_DISTANCE_FACTOR;  // 使用系数计算位置
        }

        /// <summary>
        /// 添加墙饰条
        /// 使用C# 7.3的本地函数
        /// </summary>
        private void AddWallSweep(CompoundStructure compoundStructure, UV point, string name)
        {
            var sweepInfo = CreateWallSweepInfo(true, WallSweepType.Sweep, point.V, name);
            sweepInfo.ProfileId = GetProfile(PROFILE_NAME)?.Id ?? ElementId.InvalidElementId;
            compoundStructure.AddWallSweep(sweepInfo);
        }

        /// <summary>
        /// 添加墙嵌条
        /// </summary>
        private void AddWallReveal(CompoundStructure compoundStructure, UV point, string name)
        {
            var revealInfo = CreateWallSweepInfo(true, WallSweepType.Reveal, point.U, name);
            compoundStructure.AddWallSweep(revealInfo);
        }

        /// <summary>
        /// 创建墙饰条/嵌条信息对象
        /// 使用C# 7.3的对象初始化器
        /// </summary>
        private WallSweepInfo CreateWallSweepInfo(bool isCuttable, WallSweepType sweepType, double distance, string name)
        {
            return new WallSweepInfo(isCuttable, sweepType)
            {
                DistanceMeasuredFrom = DistanceMeasuredFrom.Base,
                Distance = distance,
                WallSide = WallSide.Exterior,
                WallOffset = WALL_OFFSET,
                Id = Math.Abs(name.GetHashCode()) % 1000  // 生成唯一ID
            };
        }

        /// <summary>
        /// 创建示例材质（砖和混凝土）
        /// 使用C# 7.3的元组返回多个材质
        /// </summary>
        private (Material brickMaterial, Material concreteMaterial) CreateSampleMaterials()
        {
            var brickMaterial = CreateBrickMaterial();
            var concreteMaterial = CreateConcreteMaterial();
            return (brickMaterial, concreteMaterial);
        }

        /// <summary>
        /// 创建砖材质
        /// 使用C# 7.3的using声明和本地函数
        /// </summary>
        private Material CreateBrickMaterial()
        {
            //// 尝试获取现有材质或创建新材质
            //var existingMaterial = GetMaterialByName(BRICK_MATERIAL_NAME);
            //if (existingMaterial != null)
            //{
            //    var subTransaction = new SubTransaction(_uiDoc.Document);
            //    subTransaction.Start();
            //    var newMaterial = existingMaterial.Duplicate(BRICK_NEW_MATERIAL_NAME);
            //    newMaterial.MaterialClass = "Brick";
            //    subTransaction.Commit();
            //    return newMaterial;
            //}
            //return CreateNewMaterial("New Brick Sample", new Color(255, 0, 0), "Brick", CreateBrickStructuralAsset(), CreateBrickThermalAsset());
            return null;
        }

        /// <summary>
        /// 创建混凝土材质
        /// </summary>
        private Material CreateConcreteMaterial()
        {
            //var existingMaterial = GetMaterialByName(CONCRETE_MATERIAL_NAME);
            //if (existingMaterial != null)
            //{
            //    var subTransaction = new SubTransaction(_uiDoc.Document);
            //    subTransaction.Start();
            //    var newMaterial = existingMaterial.Duplicate(CONCRETE_NEW_MATERIAL_NAME);
            //    newMaterial.MaterialClass = "Concrete";
            //    subTransaction.Commit();
            //    return newMaterial;
            //}
            //return CreateNewMaterial("New Concrete Sample", new Color(130, 150, 120), "Concrete",
            //    CreateConcreteStructuralAsset(), CreateConcreteThermalAsset());
            return null;
        }
        /// <summary>
        /// 创建新材质（通用方法）
        /// 使用C# 7.3的using声明
        /// </summary>
        private Material CreateNewMaterial(string name, Color color, string materialClass,
            StructuralAsset structuralAsset, ThermalAsset thermalAsset)
        {
            var createMaterialTx = new SubTransaction(_uiDoc.Document);
            createMaterialTx.Start();

            var materialId = Material.Create(_uiDoc.Document, name);
            var material = _uiDoc.Document.GetElement(materialId) as Material;
            material.Color = color;

            createMaterialTx.Commit();

            // 创建属性集并关联到材质
            var createAssetTx = new SubTransaction(_uiDoc.Document);
            createAssetTx.Start();

            var structuralPse = PropertySetElement.Create(_uiDoc.Document, structuralAsset);
            var thermalPse = PropertySetElement.Create(_uiDoc.Document, thermalAsset);

            material.SetMaterialAspectByPropertySet(MaterialAspect.Structural, structuralPse.Id);
            material.SetMaterialAspectByPropertySet(MaterialAspect.Thermal, thermalPse.Id);
            material.MaterialClass = materialClass;

            createAssetTx.Commit();

            return material;
        }

        ///// <summary>
        ///// 创建砖结构材质属性
        ///// 使用C# 7.3的对象初始化器
        ///// </summary>
        //private StructuralAsset CreateBrickStructuralAsset() =>
        //    new StructuralAsset("BrickStructuralAsset", StructuralAssetClass.Generic)
        //    {
        //        //DampingRatio = 0.5
        //    };

        /// <summary>
        /// 创建砖热工材质属性
        /// </summary>
        private ThermalAsset CreateBrickThermalAsset() =>
            new ThermalAsset("BrickThermalAsset", ThermalMaterialType.Solid)
            {
                Porosity = 0.1,
                Permeability = 0.2,
                Compressibility = 0.5,
                ThermalConductivity = 0.5
            };

        ///// <summary>
        ///// 创建混凝土结构材质属性
        ///// </summary>
        //private StructuralAsset CreateConcreteStructuralAsset() =>
        //    new StructuralAsset("ConcreteStructuralAsset", StructuralAssetClass.Concrete)
        //    {
        //        ConcreteBendingReinforcement = 0.5,
        //        //DampingRatio = 0.5
        //    };

        /// <summary>
        /// 创建混凝土热工材质属性
        /// </summary>
        private ThermalAsset CreateConcreteThermalAsset() =>
            new ThermalAsset("ConcreteThermalAsset", ThermalMaterialType.Solid)
            {
                Porosity = 0.2,
                Permeability = 0.3,
                Compressibility = 0.5,
                ThermalConductivity = 0.5
            };

        /// <summary>
        /// 根据名称获取材质
        /// 使用C# 7.3的LINQ和表达式体
        /// </summary>
        private Material GetMaterialByName(string name)
        {
            var collector = new FilteredElementCollector(_uiDoc.Document);
            collector.OfClass(typeof(Material));

            return collector.Cast<Material>().FirstOrDefault(m => m.Name == name);
        }

        /// <summary>
        /// 根据名称获取轮廓族
        /// 使用C# 7.3的LINQ和表达式体
        /// </summary>
        private FamilySymbol GetProfile(string name)
        {
            var collector = new FilteredElementCollector(_uiDoc.Document);
            collector.OfCategory(BuiltInCategory.OST_ProfileFamilies);

            return collector.Cast<FamilySymbol>().FirstOrDefault(p => p.Name == name);
        }

    }

}
