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
    internal class DumpMaterialPhysicalParameters
    {
        /// <summary>
        /// 材料类型枚举 - 使用C# 7.3语法
        /// </summary>
        public enum MaterialType
        {
            Generic = 0,
            Concrete = 1,
            Steel = 2
        }
        public DumpMaterialPhysicalParameters(ExternalCommandData commandData)
        {
            string message = string.Empty;
            try
            {
                var uidoc = commandData.Application.ActiveUIDocument;

                // 验证选中元素 - 使用LINQ和模式匹配
                var selectedIds = uidoc.Selection.GetElementIds();
                if (selectedIds.Count != 1)
                {
                    message = "请仅选中一个结构构件（梁、柱或支撑）。";  return;
                }
                var selectedElement = uidoc.Document.GetElement(selectedIds.First());
                // 验证元素类型 - 使用模式匹配
                if (!(selectedElement is FamilyInstance familyInstance))
                {
                    TaskDialog.Show("Revit", "选中的元素不是族实例。");  return  ;
                }
                // 获取材料元素 - 使用LINQ和条件运算符
                var material = GetMaterialFromFamilyInstance(familyInstance, uidoc.Document);
                if (material == null)
                {
                    TaskDialog.Show("Revit", "选中的构件没有关联的结构材料。");  return  ;
                }
                // 构建属性字符串
                var resultText = BuildMaterialPropertiesReport(material);
                // 显示结果
                TaskDialog.Show("材料物理属性", resultText);
            }
            catch (Exception ex)
            {
                message = ex.Message;   return ;
            }
        }
        /// <summary>
        /// 从族实例获取材料 - 使用LINQ和表达式体
        /// </summary>
        private Material GetMaterialFromFamilyInstance(FamilyInstance instance, Document doc)
        {
            var materialParam = instance.Parameters
                .Cast<Parameter>()
                .FirstOrDefault(p => p.Definition?.Name == "Structural Material");

            if (materialParam == null) return null;

            var materialId = materialParam.AsElementId();
            return materialId != ElementId.InvalidElementId
                ? doc.GetElement(materialId) as Material
                : null;
        }

        /// <summary>
        /// 构建材料属性报告 - 使用字符串插值和辅助方法
        /// </summary>
        private string BuildMaterialPropertiesReport(Material material)
        {
            var materialTypeParam = material.get_Parameter(BuiltInParameter.PHY_MATERIAL_PARAM_TYPE);
            var materialType = (MaterialType)materialTypeParam.AsInteger();

            var report = new List<string>
            {
                $"材料类型: {GetMaterialTypeName(materialType)}",
                "",
                "=== 通用属性 ===",
                GetYoungsModulusString(material),
                GetPoissonRatioString(material),
                GetShearModulusString(material),
                GetThermalExpansionString(material),
                GetUnitWeightString(material),
                GetDampingRatioString(material),
                GetBehaviorString(material),
                ""
            };

            // 添加类型特定属性
            switch (materialType)
            {
                case MaterialType.Concrete:
                    report.Add("=== 混凝土特有属性 ===");
                    report.Add(GetConcreteCompressionString(material));
                    report.Add(GetLightweightString(material));
                    report.Add(GetShearReductionString(material));
                    break;

                case MaterialType.Steel:
                    report.Add("=== 钢材特有属性 ===");
                    report.Add(GetYieldStressString(material));
                    report.Add(GetTensileStrengthString(material));
                    report.Add(GetReductionFactorString(material));
                    break;
            }

            return string.Join(Environment.NewLine, report);
        }

        /// <summary>
        /// 获取材料类型名称 - 使用switch表达式(C# 8.0)或三元运算符
        /// </summary>
        private static string GetMaterialTypeName(MaterialType type)
        {
            switch (type)
            {
                case MaterialType.Generic:
                    return "通用";
                case MaterialType.Concrete:
                    return "混凝土";
                case MaterialType.Steel:
                    return "钢材";
                default:
                    return "未知";
            }
        }

        /// <summary>
        /// 获取参数数值的辅助方法 - 使用泛型和表达式体
        /// </summary>
        private static T GetParameterValue<T>(Material material, BuiltInParameter param, Func<Parameter, T> getter) =>
            material.get_Parameter(param) is Parameter p && p.HasValue ? getter(p) : default;

        /// <summary>
        /// 格式化双精度数组的辅助方法
        /// </summary>
        private static string FormatDoubleArray(double[] values) =>
            string.Join(", ", values.Select(v => v.ToString("F4")));

        #region 属性获取方法 - 使用元组和数组简化
        private string GetYoungsModulusString(Material material)
        {
            var values = GetModulusValues(material,
                BuiltInParameter.PHY_MATERIAL_PARAM_YOUNG_MOD1,
                BuiltInParameter.PHY_MATERIAL_PARAM_YOUNG_MOD2,
                BuiltInParameter.PHY_MATERIAL_PARAM_YOUNG_MOD3);
            return $"杨氏模量 (Ex, Ey, Ez): {FormatDoubleArray(values)}";
        }

        private string GetPoissonRatioString(Material material)
        {
            var values = GetModulusValues(material,
                BuiltInParameter.PHY_MATERIAL_PARAM_POISSON_MOD1,
                BuiltInParameter.PHY_MATERIAL_PARAM_POISSON_MOD2,
                BuiltInParameter.PHY_MATERIAL_PARAM_POISSON_MOD3);
            return $"泊松比 (νxy, νyz, νzx): {FormatDoubleArray(values)}";
        }

        private string GetShearModulusString(Material material)
        {
            var values = GetModulusValues(material,
                BuiltInParameter.PHY_MATERIAL_PARAM_SHEAR_MOD1,
                BuiltInParameter.PHY_MATERIAL_PARAM_SHEAR_MOD2,
                BuiltInParameter.PHY_MATERIAL_PARAM_SHEAR_MOD3);
            return $"剪切模量 (Gxy, Gyz, Gzx): {FormatDoubleArray(values)}";
        }

        private string GetThermalExpansionString(Material material)
        {
            var values = GetModulusValues(material,
                BuiltInParameter.PHY_MATERIAL_PARAM_EXP_COEFF1,
                BuiltInParameter.PHY_MATERIAL_PARAM_EXP_COEFF2,
                BuiltInParameter.PHY_MATERIAL_PARAM_EXP_COEFF3);
            return $"热膨胀系数 (αx, αy, αz): {FormatDoubleArray(values)}";
        }

        /// <summary>
        /// 获取三个方向属性值的通用方法 - 使用params数组
        /// </summary>
        private double[] GetModulusValues(Material material, params BuiltInParameter[] parameters)
        {
            var result = new double[3];
            for (int i = 0; i < Math.Min(parameters.Length, 3); i++)
            {
                result[i] = material.get_Parameter(parameters[i])?.AsDouble() ?? 0;
            }
            return result;
        }

        private string GetUnitWeightString(Material material)
        {
            var value = GetParameterValue(material, BuiltInParameter.PHY_MATERIAL_PARAM_UNIT_WEIGHT, p => p.AsDouble());
            return $"单位重量: {value:F4}";
        }

        private string GetDampingRatioString(Material material)
        {
            //var value = GetParameterValue(material, BuiltInParameter.PHY_MATERIAL_PARAM_DAMPING_RATIO, p => p.AsDouble());
            //return $"阻尼比: {value:F4}";
            return string.Empty;
        }

        private string GetBehaviorString(Material material)
        {
            var behavior = GetParameterValue(material, BuiltInParameter.PHY_MATERIAL_PARAM_BEHAVIOR, p => p.AsInteger());
            var behaviorText = behavior == 0 ? "各向同性" : (behavior == 1 ? "正交各向异性" : "未知");
            return $"材料行为: {behaviorText}";
        }

        private string GetConcreteCompressionString(Material material)
        {
            var value = GetParameterValue(material, BuiltInParameter.PHY_MATERIAL_PARAM_CONCRETE_COMPRESSION, p => p.AsDouble());
            return $"混凝土抗压强度: {value:F4}";
        }

        private string GetLightweightString(Material material)
        {
            var value = GetParameterValue(material, BuiltInParameter.PHY_MATERIAL_PARAM_LIGHT_WEIGHT, p => p.AsDouble());
            var isLightweight = Math.Abs(value - 1) < 0.001;
            return $"轻质混凝土: {(isLightweight ? "是" : "否")}";
        }

        private string GetShearReductionString(Material material)
        {
            var value = GetParameterValue(material, BuiltInParameter.PHY_MATERIAL_PARAM_SHEAR_STRENGTH_REDUCTION, p => p.AsDouble());
            return $"抗剪强度折减系数: {value:F4}";
        }

        private string GetYieldStressString(Material material)
        {
            var value = GetParameterValue(material, BuiltInParameter.PHY_MATERIAL_PARAM_MINIMUM_YIELD_STRESS, p => p.AsDouble());
            return $"最小屈服应力: {value:F4}";
        }

        private string GetTensileStrengthString(Material material)
        {
            var value = GetParameterValue(material, BuiltInParameter.PHY_MATERIAL_PARAM_MINIMUM_TENSILE_STRENGTH, p => p.AsDouble());
            return $"最小抗拉强度: {value:F4}";
        }

        private string GetReductionFactorString(Material material)
        {
            var value = GetParameterValue(material, BuiltInParameter.PHY_MATERIAL_PARAM_REDUCTION_FACTOR, p => p.AsDouble());
            return $"折减系数: {value:F4}";
        }
        #endregion
    }

}
