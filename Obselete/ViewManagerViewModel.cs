using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using CreatePipe.models;
using CreatePipe.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;


namespace CreatePipe
{

    public class ViewManagerViewModel : ObserverableObject
    {
        public Document Document { get; set; }
        public UIDocument uIDoc { get; set; }
        public View ActiveView { get; set; }
        //private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
        public ViewManagerViewModel(UIApplication application)
        {
            Document = application.ActiveUIDocument.Document;
            uIDoc = application.ActiveUIDocument;
            views = new FilteredElementCollector(Document).OfCategory(BuiltInCategory.OST_Views).OfClass(typeof(View)).Cast<View>().ToList();
            ActiveView = Document.ActiveView;
            QueryELement(null);
        }
        public ICommand NewLevelViewCommand => new BaseBindingCommand(NewLevelView);
        private void NewLevelView(Object para)
        {
            Document.NewTransaction(() =>
            {
                int newView = 0;
                var levels = new FilteredElementCollector(Document).OfClass(typeof(Level)).Cast<Level>().ToList();
                // 获取已有平面视图的标高ID
                var usedLevelIds = new FilteredElementCollector(Document).OfClass(typeof(ViewPlan)).Cast<ViewPlan>()
                    .Select(v => v.GenLevel?.Id)
                    .Where(id => id != null).ToHashSet();
                // 筛选未使用的标高
                var unusedLevels = levels.Where(l => !usedLevelIds.Contains(l.Id)).ToList();
                if (unusedLevels.Count == 0)
                {
                    TaskDialog.Show("完成", "所有标高都已有关联平面视图");
                    return;
                }
                else
                {
                    foreach (Level level in unusedLevels)
                    {
                        try
                        {
                            // 创建楼层平面
                            ElementId floorPlanTypeId = Document.GetDefaultElementTypeId(ElementTypeGroup.ViewTypeFloorPlan);
                            ViewPlan floorPlan = ViewPlan.Create(Document, floorPlanTypeId, level.Id);
                            floorPlan.Name = $"{level.Name} - 楼层平面";
                            newView++;
                        }
                        catch (Exception ex)
                        {
                            TaskDialog.Show("错误", $"为标高 {level.Name} 创建平面失败: {ex.Message}");
                        }
                    }
                    TaskDialog.Show("tt", $"新建平面标高视图{newView}个");
                }
            }, "新建标高视图");
        }
        //避免名称重复
        string GenerateUniqueViewName(string baseName)
        {
            string newName = baseName;
            int counter = 1;
            while (new FilteredElementCollector(Document).OfClass(typeof(View)).Any(x => x.Name.Equals(newName)))
            {
                newName = $"{baseName} ({counter++})";
            }
            return newName;
        }
        public ICommand RemoveViewTemplateCommand => new RelayCommand<IEnumerable<object>>(RemoveViewTemplate);
        private void RemoveViewTemplate(IEnumerable<object> selectedElements)
        {
            if (selectedElements == null) return;
            List<ElementId> toRemove = selectedElements.Cast<ViewTemplateEntity>()
                .Select(item => item.Id).ToList();
            TaskDialog td = new TaskDialog("重要提示")
            {
                MainInstruction = "警告",
                MainContent = "请谨慎选择要删除对象，操作不可撤销，确定删除请点击下方选项继续",
                MainIcon = TaskDialogIcon.TaskDialogIconWarning,
                CommonButtons = TaskDialogCommonButtons.Close,
                DefaultButton = TaskDialogResult.Close
                //ExpandedContent ="test",                    
                //CommonButtons = TaskDialogCommonButtons.Close | TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No,
                //DefaultButton = TaskDialogResult.No
            };
            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "删除选中视图的所有过滤器设置");
            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "删除选中视图的视图样板设置");
            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink3, "删除选中视图的过滤器设置以及视图样板设置");
            TaskDialogResult result = td.Show();
            if (TaskDialogResult.CommandLink1 == result)
            {
                Document.NewTransaction(() =>
                {
                    foreach (ElementId item in toRemove)
                    {
                        View view = (View)Document.GetElement(item);
                        var elementIds = view.GetFilters();
                        if (elementIds.Count() == 0) return;
                        foreach (var item2 in elementIds)
                        {
                            view.RemoveFilter(item2);
                        }
                    }
                }, "删除多个视图过滤器");
            }
            else if (TaskDialogResult.CommandLink2 == result)
            {
                Document.NewTransaction(() =>
                {
                    foreach (ElementId item in toRemove)
                    {
                        View view = (View)Document.GetElement(item);
                        if (view.ViewTemplateId.IntegerValue == -1) return;
                        view.ViewTemplateId = ElementId.InvalidElementId;
                    }
                }, "删除多个视图视图样板");
            }
            else if (TaskDialogResult.CommandLink3 == result)
            {
                Document.NewTransaction(() =>
                {
                    foreach (ElementId item in toRemove)
                    {
                        View view = (View)Document.GetElement(item);
                        var elementIds = view.GetFilters();
                        foreach (var item2 in elementIds)
                        {
                            view.RemoveFilter(item2);
                        }
                        view.ViewTemplateId = ElementId.InvalidElementId;
                    }
                }, "删除多个视图过滤器和视图样板");
            }
        }
        public ICommand DeleteElementsCommand => new RelayCommand<IEnumerable<object>>(DeleteELements);
        private void DeleteELements(IEnumerable<object> selectedElements)
        {
            Document.NewTransaction(() =>
            {
                List<ElementId> toRemove = new List<ElementId>();
                List<ViewTemplateEntity> selectedItems = selectedElements.Cast<ViewTemplateEntity>().ToList();
                if (selectedItems == null) return;
                foreach (var item in selectedItems)
                {
                    if (item.Id == ActiveView.Id) return;
                    toRemove.Add(item.Id);
                    AllViews.Remove(item);
                }
                Document.Delete(toRemove);
            }, "删除多个视图");
        }
        public ICommand DeleteViewCommand => new RelayCommand<ViewTemplateEntity>(DeleteElement);
        public void DeleteElement(ViewTemplateEntity vt)
        {
            if (vt.Id == ActiveView.Id) return;
            Document.NewTransaction(() =>
            {
                Document.Delete(vt.Id);
                AllViews.Remove(vt);
            }, "删除视图");
        }
        public ICommand FindViewCommand => new RelayCommand<ViewTemplateEntity>(ActivateView);
        private void ActivateView(ViewTemplateEntity entity)
        {
            View view = entity.Document.GetElement(entity.Id) as View;
            if (view != null) uIDoc.ActiveView = view;
        }
        public ICommand QueryElementCommand => new RelayCommand<string>(QueryELement);
        private void QueryELement(string obj)
        {
            AllViews.Clear();
            List<ViewTemplateEntity> vte = views.Select(v => new ViewTemplateEntity(v)).Where(e => e.isTemplate != true && (string.IsNullOrEmpty(obj) || e.ViewName.Contains(obj) || e.ViewName.IndexOf(obj, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
            foreach (var item in vte)
            {
                AllViews.Add(item);
            }
        }
        public List<Autodesk.Revit.DB.View> views = new List<View>();
        private ObservableCollection<ViewTemplateEntity> allViews = new ObservableCollection<ViewTemplateEntity>();
        public ObservableCollection<ViewTemplateEntity> AllViews
        {
            get => allViews;
            set => SetProperty(ref allViews, value);
        }
    }
}
