using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.UI;
using System.Linq;

namespace CreatePipe.OfficalSamples
{
    internal class ExtensibleStorageDeletion
    {
        public ExtensibleStorageDeletion(ExternalCommandData commandData)
        {
            string message = string.Empty;
            if (commandData.Application.ActiveUIDocument.Document.IsReadOnly)
            {
                message = "文档处于只读状态。"; return;
            }
            var schemas = Schema.ListSchemas();
            if (schemas.Count == 0)
            {
                message = "没有可用的 Schema。"; return;
            }
            // 构建 Schema 选择列表
            var schemaOptions = schemas
                .Select((s, i) => $"{i + 1}. {s.SchemaName} (GUID: {s.GUID})").ToList();
            schemaOptions.Insert(0, "0. 删除所有 Schema");
            schemaOptions.Add("c. 取消操作");
            var schemaList = string.Join("\n", schemaOptions);
            var selection = TaskDialog.Show("选择要删除的 Schema",
                $"请选择要删除的 Schema：\n\n{schemaList}\n\n请输入选项编号：",
                TaskDialogCommonButtons.Ok);
            // 注意：TaskDialog 不支持输入，实际应用中应使用自定义对话框
            // 这里简化为演示逻辑结构


        }
    }
}
