using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.UI;
using System;
using System.Linq;

namespace CreatePipe.OfficalSamples
{
    internal class ScheduleFormatterCommand
    {
        private static readonly Guid SchemaId = new Guid("98017A5F-F4A7-451C-8807-EF137B587C50");
        private static readonly Guid UpdaterId = new Guid("{C8483107-EF6D-4FDB-BB88-AF79E0E62361}");
        public ScheduleFormatterCommand(ExternalCommandData commandData)
        {
            var viewSchedule = commandData.View as ViewSchedule;
            string message = string.Empty;
            if (viewSchedule == null)
            {
                message = "请选中一个明细表视图。"; return;
            }
            var formatter = GetFormatter(commandData);
            var schema = GetOrCreateSchema();
            using (var transaction = new Transaction(viewSchedule.Document, "格式化明细表列"))
            {
                transaction.Start();
                // 应用格式
                formatter.FormatScheduleColumns(viewSchedule);
                // 添加标记
                AddFormattingMarker(viewSchedule, schema);
                transaction.Commit();
            }
            // 注册更新器
            RegisterUpdater(commandData, formatter, schema);
        }
        /// <summary>
        /// 创建或获取格式化器实例
        /// </summary>
        private static ScheduleFormatter GetFormatter(ExternalCommandData commandData) =>
            new ScheduleFormatter
            {
                Schema = GetOrCreateSchema(),
                AddInId = commandData.Application.ActiveAddInId
            };

        /// <summary>
        /// 获取或创建数据架构
        /// </summary>
        private static Schema GetOrCreateSchema()
        {
            var schema = Schema.Lookup(SchemaId);
            if (schema != null) return schema;

            var builder = new SchemaBuilder(SchemaId);
            builder.SetSchemaName("ScheduleFormatterFlag");
            builder.AddSimpleField("Formatted", typeof(bool));

            return builder.Finish();
        }

        /// <summary>
        /// 添加格式化标记到明细表
        /// </summary>
        private static void AddFormattingMarker(ViewSchedule schedule, Schema schema)
        {
            var entity = schedule.GetEntity(schema);
            if (entity.IsValid()) return;

            var newEntity = new Entity(schema);
            newEntity.Set("Formatted", true);
            schedule.SetEntity(newEntity);
        }

        /// <summary>
        /// 注册更新器 - 用于监听明细表变化
        /// </summary>
        private static void RegisterUpdater(ExternalCommandData commandData, ScheduleFormatter formatter, Schema schema)
        {
            var updaterId = formatter.GetUpdaterId();

            if (UpdaterRegistry.IsUpdaterRegistered(updaterId)) return;

            // 创建过滤器：明细表类型 + 带有目标Schema的扩展存储
            var classFilter = new ElementClassFilter(typeof(ViewSchedule));
            var esFilter = new ExtensibleStorageFilter(schema.GUID);
            var filter = new LogicalAndFilter(classFilter, esFilter);

            // 注册更新器并添加触发条件
            UpdaterRegistry.RegisterUpdater(formatter);
            UpdaterRegistry.AddTrigger(updaterId, filter, Element.GetChangeTypeAny());
        }

        /// <summary>
        /// 设置更新器（用于应用启动时注册）
        /// </summary>
        public static void SetupUpdater(UIControlledApplication application)
        {
            var schema = GetOrCreateSchema();
            var formatter = new ScheduleFormatter
            {
                Schema = schema,
                AddInId = application.ActiveAddInId
            };

            RegisterUpdater(application, formatter, schema);
        }

        private static void RegisterUpdater(UIControlledApplication application, ScheduleFormatter formatter, Schema schema)
        {
            var updaterId = formatter.GetUpdaterId();
            if (UpdaterRegistry.IsUpdaterRegistered(updaterId)) return;

            var classFilter = new ElementClassFilter(typeof(ViewSchedule));
            var esFilter = new ExtensibleStorageFilter(schema.GUID);
            var filter = new LogicalAndFilter(classFilter, esFilter);

            UpdaterRegistry.RegisterUpdater(formatter);
            UpdaterRegistry.AddTrigger(updaterId, filter, Element.GetChangeTypeAny());
        }
    }
    /// <summary>
    /// 明细表格式化器 - 实现IUpdater接口，自动监听并格式化明细表
    /// </summary>
    public class ScheduleFormatter : IUpdater
    {
        private static readonly Guid UpdaterGuid = new Guid("{C8483107-EF6D-4FDB-BB88-AF79E0E62361}");

        public Schema Schema { get; set; }
        public AddInId AddInId { get; set; }

        #region IUpdater 实现
        public void Execute(UpdaterData data)
        {
            // 只处理已格式化的明细表
            foreach (var scheduleId in data.GetModifiedElementIds())
            {
                if (data.GetDocument().GetElement(scheduleId) is ViewSchedule schedule)
                {
                    FormatScheduleColumns(schedule);
                }
            }
        }

        public string GetAdditionalInformation() => "自动明细表格式化器";
        public ChangePriority GetChangePriority() => ChangePriority.Views;
        public string GetUpdaterName() => "AutomaticScheduleFormatter";
        public UpdaterId GetUpdaterId() => new UpdaterId(AddInId, UpdaterGuid);
        #endregion

        /// <summary>
        /// 格式化明细表列 - 应用交替背景色
        /// </summary>
        public void FormatScheduleColumns(ViewSchedule schedule)
        {
            var definition = schedule.Definition;
            var fieldOrder = definition.GetFieldOrder().ToList();

            if (!fieldOrder.Any()) return;

            var colors = GetAlternatingColors();

            for (int i = 0; i < fieldOrder.Count; i++)
            {
                var fieldId = fieldOrder[i];
                var field = definition.GetField(fieldId);
                var style = field.GetStyle();
                var options = style.GetCellStyleOverrideOptions();

                // 设置背景色覆盖选项
                options.BackgroundColor = true;
                style.SetCellStyleOverrideOptions(options);

                // 设置背景色（交替索引）
                style.BackgroundColor = i % 2 == 0 ? colors.Highlight : colors.White;

                field.SetStyle(style);
            }
        }

        /// <summary>
        /// 获取交替背景色 - 使用元组返回值
        /// </summary>
        private static (Color White, Color Highlight) GetAlternatingColors()
        {
            return (
                White: new Color(0xFF, 0xFF, 0xFF),
                Highlight: new Color(0xD8, 0xD8, 0xD8)
            );
        }
    }
}
