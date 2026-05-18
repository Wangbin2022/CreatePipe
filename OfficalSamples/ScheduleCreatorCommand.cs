using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CreatePipe.OfficalSamples
{
    internal class ScheduleCreatorCommand
    {
        // 需要跳过的参数列表 - 使用HashSet提高查找效率
        private static readonly HashSet<ElementId> SkipParameterIds = new HashSet<ElementId>
        {
            new ElementId(BuiltInParameter.ALL_MODEL_MARK)
        };

        // 体积过滤阈值（立方英尺转换为英尺）
        //private const double VolumeFilterThreshold = 0.8 * Math.Pow(3.2808399, 3.0);
        private const double VolumeFilterThreshold = 0.018; //随手写的

        // 偏移量：2英寸（转换为英尺）
        private static readonly XYZ Offset = new XYZ(2.0 / 12.0, -2.0 / 12.0, 0);

        /// <summary>
        /// 创建明细表并添加到图纸
        /// </summary>
        public void CreateAndAddSchedules(UIDocument uiDoc)
        {
            using (var tGroup = new TransactionGroup(uiDoc.Document, "创建明细表和图纸"))
            {
                tGroup.Start();

                var schedules = CreateSchedules(uiDoc);

                foreach (var schedule in schedules)
                {
                    AddScheduleToNewSheet(uiDoc.Document, schedule);
                }

                tGroup.Assimilate();
            }
        }

        /// <summary>
        /// 创建墙体明细表 - 添加字段、过滤条件和排序
        /// </summary>
        private ICollection<ViewSchedule> CreateSchedules(UIDocument uiDoc)
        {
            var doc = uiDoc.Document;
            var schedules = new List<ViewSchedule>();

            using (var transaction = new Transaction(doc, "创建明细表"))
            {
                transaction.Start();

                var schedule = CreateWallSchedule(doc);
                schedules.Add(schedule);

                transaction.Commit();
            }

            // 设置当前视图为新创建的明细表
            uiDoc.ActiveView = schedules[0];

            return schedules;
        }

        /// <summary>
        /// 创建墙体明细表并配置所有字段
        /// </summary>
        private ViewSchedule CreateWallSchedule(Document doc)
        {
            var schedule = ViewSchedule.CreateSchedule(doc,
                new ElementId(BuiltInCategory.OST_Walls),
                ElementId.InvalidElementId);
            schedule.Name = "墙体明细表 1";

            var definition = schedule.Definition;

            foreach (var fieldInfo in GetFieldConfigurations(definition, doc))
            {
                var field = definition.AddField(fieldInfo.SchedulableField);
                ApplyFieldFormatting(field, fieldInfo.ParameterId, doc);
                ApplyFilterIfVolume(field, definition);
                ApplySortGroupingIfType(field, definition);
            }

            return schedule;
        }

        /// <summary>
        /// 字段配置信息 - 使用元组
        /// </summary>
        private IEnumerable<(SchedulableField SchedulableField, ElementId ParameterId)>
            GetFieldConfigurations(ScheduleDefinition definition, Document doc)
        {
            return definition.GetSchedulableFields()
                .Where(f => f.FieldType == ScheduleFieldType.Instance)
                .Select(f => (Field: f, ParamId: f.ParameterId))
                .Where(t => !SkipParameterIds.Contains(t.ParamId));
        }

        /// <summary>
        /// 应用字段格式 - 根据参数类型设置列宽和对齐方式
        /// </summary>
        private void ApplyFieldFormatting(ScheduleField field, ElementId paramId, Document doc)
        {
            // 检查是否为内置参数
            if (!Enum.IsDefined(typeof(BuiltInParameter), paramId.IntegerValue)) return;

            var bip = (BuiltInParameter)paramId.IntegerValue;
            var storageType = doc.get_TypeOfStorage(bip);

            switch (storageType)
            {
                case StorageType.String:
                case StorageType.ElementId:
                    field.GridColumnWidth = 3 * field.GridColumnWidth;
                    field.HorizontalAlignment = ScheduleHorizontalAlignment.Left;
                    break;
                default:
                    field.HorizontalAlignment = ScheduleHorizontalAlignment.Center;
                    break;
            }
        }

        /// <summary>
        /// 如果是体积参数，添加体积过滤条件
        /// </summary>
        private void ApplyFilterIfVolume(ScheduleField field, ScheduleDefinition definition)
        {
            if (field.ParameterId == new ElementId(BuiltInParameter.HOST_VOLUME_COMPUTED))
            {
                var filter = new ScheduleFilter(field.FieldId,
                    ScheduleFilterType.GreaterThan,
                    VolumeFilterThreshold);
                definition.AddFilter(filter);
            }
        }

        /// <summary>
        /// 如果是类型参数，添加分组排序
        /// </summary>
        private void ApplySortGroupingIfType(ScheduleField field, ScheduleDefinition definition)
        {
            if (field.ParameterId == new ElementId(BuiltInParameter.ELEM_TYPE_PARAM))
            {
                var sortGroup = new ScheduleSortGroupField(field.FieldId)
                {
                    ShowHeader = true
                };
                definition.AddSortGroupField(sortGroup);
            }
        }

        /// <summary>
        /// 将明细表添加到新图纸
        /// </summary>
        private void AddScheduleToNewSheet(Document doc, ViewSchedule schedule)
        {
            using (var transaction = new Transaction(doc, "创建图纸并放置明细表"))
            {
                transaction.Start();

                // 获取第一个标题栏类型
                var titleBlockId = GetFirstTitleBlockTypeId(doc);
                if (titleBlockId == ElementId.InvalidElementId)
                {
                    transaction.RollBack();
                    return;
                }

                // 创建图纸
                var newSheet = ViewSheet.Create(doc, titleBlockId);
                newSheet.Name = $"图纸 - {schedule.Name}";

                doc.Regenerate();

                // 计算放置位置
                var position = CalculateSchedulePosition(doc, newSheet, titleBlockId);

                // 放置明细表实例
                var placedInstance = ScheduleSheetInstance.Create(doc, newSheet.Id, schedule.Id, position);

                transaction.Commit();
            }
        }

        /// <summary>
        /// 获取第一个标题栏类型的ElementId
        /// </summary>
        private static ElementId GetFirstTitleBlockTypeId(Document doc) =>
            new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                .WhereElementIsElementType()
                .FirstElementId();

        /// <summary>
        /// 计算明细表在图纸上的放置位置
        /// </summary>
        private XYZ CalculateSchedulePosition(Document doc, ViewSheet sheet, ElementId titleBlockId)
        {
            // 查找图纸上的标题栏实例
            var titleBlock = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                .OwnedByView(sheet.Id)
                .FirstElement();

            if (titleBlock == null) return new XYZ(0, 0, 0);

            // 获取标题栏边界框
            var bbox = titleBlock.get_BoundingBox(sheet);

            // 计算右上角位置并偏移
            var upperRight = new XYZ(bbox.Max.X, bbox.Max.Y, bbox.Min.Z);
            return upperRight + Offset;
        }
        public ScheduleCreatorCommand(ExternalCommandData commandData)
        {

        }
    }
}
