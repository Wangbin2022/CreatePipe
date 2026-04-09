using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CreatePipe.cmd;
using CreatePipe.models;
using CreatePipe.Utils;
using CreatePipe.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CreatePipe.Form
{
    /// <summary>
    /// GuidanaceSignManagerView.xaml 的交互逻辑
    /// </summary>
    public partial class GuidanceSignManagerView : Window
    {
        public GuidanceSignManagerView(UIApplication uiApp)
        {
            InitializeComponent();
            this.DataContext = new GuidanaceSignManagerViewModel(uiApp);
        }
        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
    public class GuidanaceSignManagerViewModel : ObserverableObject, IQueryViewModelWithDelete<GuidanceSignEntity>
    {
        public Document Document { get; set; }
        public UIDocument UIDoc { get; set; }
        // 4. 通用外部事件处理器
        public BaseExternalHandler ExternalHandler { get; } = new BaseExternalHandler();
        // 2. 核心数据集合 (取代原有的 AllSigns)
        private ObservableCollection<GuidanceSignEntity> _collection = new ObservableCollection<GuidanceSignEntity>();
        public ObservableCollection<GuidanceSignEntity> Collection
        {
            get => _collection;
            set => SetProperty(ref _collection, value);
        }
        // Parameters for editing sign content
        public int SignRows { get; set; } = 1;
        private string frontSignFirst = "-";
        private string frontSignSecond = "-";
        private string frontSignThird = "-";
        private string backSignFirst = "-";
        private string backSignSecond = "-";
        private string backSignThird = "-";
        public string FrontSignFirst { get => frontSignFirst; set => SetProperty(ref frontSignFirst, value); }
        public string FrontSignSecond { get => frontSignSecond; set => SetProperty(ref frontSignSecond, value); }
        public string FrontSignThird { get => frontSignThird; set => SetProperty(ref frontSignThird, value); }
        public string BackSignFirst { get => backSignFirst; set => SetProperty(ref backSignFirst, value); }
        public string BackSignSecond { get => backSignSecond; set => SetProperty(ref backSignSecond, value); }
        public string BackSignThird { get => backSignThird; set => SetProperty(ref backSignThird, value); }
        public GuidanaceSignManagerViewModel(UIApplication application)
        {
            UIDoc = application.ActiveUIDocument;
            Document = UIDoc.Document;
            InitFunc();
        }
        // 1. 初始化与数据加载
        public void InitFunc()
        {
            QueryElement(null);
            if (Collection.Count == 0)
            {
                TaskDialog.Show("提示", "当前模型中没有找到标识牌标记。");
            }
        }
        public ICommand QueryElementCommand => new RelayCommand<string>(QueryElement);
        public void QueryElement(string text)
        {
            //ExternalHandler.Run(app =>
            //{
            //    Collection.Clear();
            //    var guidanceSigns = new FilteredElementCollector(Document).OfClass(typeof(IndependentTag)).Cast<IndependentTag>().Where(s => s.Name.StartsWith("标记_标识")).ToList();
            //    foreach (var sign in guidanceSigns)
            //    {
            //        if (string.IsNullOrWhiteSpace(text) || sign.TagText.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
            //        {
            //            Collection.Add(new GuidanceSignEntity(sign));
            //        }
            //    }
            //    //RefreshDuplicateCheck();
            //});

            var tempEntities = new List<GuidanceSignEntity>();
            var tags = new FilteredElementCollector(Document)
                .OfClass(typeof(IndependentTag)).Cast<IndependentTag>()
                .Where(t => t.Name.StartsWith("标记_标识") && t.TaggedLocalElementId != ElementId.InvalidElementId).ToList();
            foreach (var tag in tags)
            {
                try
                {
                    // Check if the tagged element exists and is a FamilyInstance before creating GuidanceSignEntity
                    if (Document.GetElement(tag.TaggedLocalElementId) is FamilyInstance taggedInstance)
                    {
                        if (string.IsNullOrWhiteSpace(text) ||
                            tag.TagText.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            tempEntities.Add(new GuidanceSignEntity(tag));
                        }
                    }
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("tt", $"Error loading GuidanceSignEntity for tag ID {tag.Id}: {ex.Message}");
                }
            }
            // Perform duplicate check on the collected entities
            CheckForDuplicateSerials(tempEntities);
            // Update the ObservableCollection on the UI thread
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                Collection.Clear();
                foreach (var entity in tempEntities)
                {
                    Collection.Add(entity);
                }
            });
        }
        public ICommand DeleteElementCommand => new RelayCommand<GuidanceSignEntity>(DeleteElement);
        public void DeleteElement(GuidanceSignEntity entity)
        {
            if (entity == null) return;
            DeleteElements(new List<object> { entity });
        }
        public ICommand DeleteElementsCommand => new RelayCommand<IEnumerable<object>>(DeleteElements);
        public void DeleteElements(IEnumerable<object> selectedItems)
        {
            if (selectedItems == null || !selectedItems.Any()) return;

            var itemsToDelete = selectedItems.Cast<GuidanceSignEntity>().ToList();
            if (itemsToDelete.Count == 0) return;

            ExternalHandler.Run(app =>
            {
                var idsToDelete = new List<ElementId>();
                // Collect both the IndependentTag ID and the FamilyInstance ID to delete
                foreach (var item in itemsToDelete)
                {
                    idsToDelete.Add(item.Id); // The tag itself
                    idsToDelete.Add(item.EntityId); // The tagged FamilyInstance
                }

                try
                {
                    // Use a single transaction for all deletions
                    NewTransaction.Execute(Document, "批量删除标识牌及标记", () =>
                    {
                        Document.Delete(idsToDelete);
                    });

                    // Update UI Collection on the UI thread after successful Revit transaction
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (var item in itemsToDelete)
                        {
                            Collection.Remove(item);
                        }
                        TaskDialog.Show("删除成功", $"成功删除了 {itemsToDelete.Count} 个标识牌及其标记。");
                    });
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        TaskDialog.Show("删除失败", $"删除过程中发生错误: {ex.Message}");
                    });
                }
            });
        }
        private void CheckForDuplicateSerials(IEnumerable<GuidanceSignEntity> entities)
        {
            // Step 1: 按 TagName 分组
            var duplicateGroups = entities.GroupBy(e => e.TagName).Where(g => g.Count() > 1)
                .SelectMany(g => g).Select(e => e.TagName).ToHashSet();
            // Step 2: 回填到实体
            foreach (var entity in entities)
            {
                entity.HasSameSerial = duplicateGroups.Contains(entity.TagName);
            }
        }
        public ICommand UpdateSerialCodeCommand => new RelayCommand<GuidanceSignEntity>(ExecuteUpdateSerialCode);
        private void ExecuteUpdateSerialCode(GuidanceSignEntity entity)
        {
            if (entity == null) return;

            // Temporarily store the new value from UI, assuming this method is triggered by a UI command
            // and the entity.SerialCode is already updated by binding.
            string newSerialCode = entity.SerialCode; // Get the new value from the entity

            ExternalHandler.Run(app =>
            {
                try
                {
                    // Start a transaction to modify the Revit element
                    NewTransaction.Execute(Document, "更新标识牌编号", () =>
                    {
                        entity.Entity.LookupParameter("本层编号")?.Set(newSerialCode);
                    });

                    // Update the entity's property in UI thread to ensure consistent state
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        // This is crucial: update the entity's internal _serialCode
                        // and trigger OnPropertyChanged for SerialCode and TagName
                        entity.SetSerialCode(newSerialCode);
                        CheckForDuplicateSerials(Collection); // Re-check duplicates after a serial code changes
                    });
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        TaskDialog.Show("错误", $"更新编号失败: {ex.Message}");
                        // Optionally, revert UI change if Revit operation failed
                        QueryElement(null); // Reload to reset the UI to actual model state
                    });
                }
            });
        }
        public ICommand UpdateEntityLengthCommand => new RelayCommand<GuidanceSignEntity>(ExecuteUpdateEntityLength);
        private void ExecuteUpdateEntityLength(GuidanceSignEntity entity)
        {
            if (entity == null) return;
            double newLengthMm = entity.EntityLength; 
            ExternalHandler.Run(app =>
            {
                try
                {
                    NewTransaction.Execute(Document, "更新标识长度", () =>
                    {
                        // Revit uses internal units (feet), convert mm to feet
                        entity.Entity.LookupParameter("推荐长度")?.Set(newLengthMm / 304.8);
                    });
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        entity.SetEntityLength(newLengthMm);
                    });
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        TaskDialog.Show("错误", $"更新长度失败: {ex.Message}");
                        QueryElement(null); // Reload to reset the UI
                    });
                }
            });
        }
        public ICommand EditContentCommand => new RelayCommand<GuidanceSignEntity>(EditContent);
        private void EditContent(GuidanceSignEntity entity)
        {
            if (entity == null) return;

            UniversalNewString subView = new UniversalNewString("请按规则修改标牌文字，保存更新", entity.EntityContent);
            if (subView.ShowDialog() != true || !(subView.DataContext is NewStringViewModel vm) || string.IsNullOrWhiteSpace(vm.NewName))
            {
                return;
            }

            string newEditContent = vm.NewName;
            if (newEditContent == entity.EntityContent) return;

            // Parse content in UI thread
            string frontContent = null;
            string backContent = null;
            if (newEditContent.Contains("|"))
            {
                string[] parts = newEditContent.Split(new[] { '|' }, 2, StringSplitOptions.None);
                frontContent = parts[0].Trim();
                backContent = parts.Length > 1 ? parts[1].Trim() : string.Empty;
            }
            else
            {
                frontContent = newEditContent;
            }

            string[] frontParts = frontContent.Split(new[] { ';', '；' }, StringSplitOptions.RemoveEmptyEntries);
            int frontCount = Math.Min(frontParts.Length, 3);
            FrontSignFirst = (frontCount > 0) ? RemovePrefix(frontParts[0].Trim()) : "-";
            FrontSignSecond = (frontCount > 1) ? frontParts[1].Trim() : "-";
            FrontSignThird = (frontCount > 2) ? frontParts[2].Trim() : "-";

            int backCount = 0;
            if (!string.IsNullOrEmpty(backContent))
            {
                string[] backParts = backContent.Split(new[] { ';', '；' }, StringSplitOptions.RemoveEmptyEntries);
                backCount = Math.Min(backParts.Length, 3);
                BackSignFirst = (backCount > 0) ? RemovePrefix(backParts[0].Trim()) : "-";
                BackSignSecond = (backCount > 1) ? backParts[1].Trim() : "-";
                BackSignThird = (backCount > 2) ? backParts[2].Trim() : "-";
            }
            else
            {
                BackSignFirst = "-";
                BackSignSecond = "-";
                BackSignThird = "-";
            }
            SignRows = Math.Max(frontCount, backCount);

            ExternalHandler.Run(app =>
            {
                try
                {
                    NewTransaction.Execute(Document, "修改标识文字", () =>
                    {
                        SetSignParameters(entity.Entity, entity.IsDouble);
                        // Also update the direct "标识内容" parameter if it exists
                        entity.Entity.LookupParameter("标识内容")?.Set(newEditContent);
                    });

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        entity.EntityContent = newEditContent; // Update UI bound property
                    });
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        TaskDialog.Show("错误", $"修改标识文字失败: {ex.Message}");
                        // Revert UI to model state if transaction fails
                        QueryElement(null);
                    });
                }
            });
        }
        private void SetSignParameters(FamilyInstance instance, bool isDouble)
        {
            // Using a dictionary to map parameter names for cleaner access
            Dictionary<int, string> frontTextParams = new Dictionary<int, string>
        {
            {1, "文字转换"}, {2, "文字转换 第二行"}, {3, "文字转换 第三行"}
        };
            Dictionary<int, string> backTextParams = new Dictionary<int, string>
        {
            {1, "文字转换 背面"}, {2, "文字转换 第二行背面"}, {3, "文字转换 第三行背面"}
        };
            Dictionary<int, string> countParams = new Dictionary<int, string>
        {
            {1, "推荐数量 1块"}, {2, "推荐数量 2块"}, {3, "推荐数量 3块"}
        };

            // Reset all count parameters to 0 first
            foreach (var paramName in countParams.Values)
            {
                instance.LookupParameter(paramName)?.Set(0);
            }

            // Set counts based on SignRows
            for (int i = 1; i <= SignRows; i++)
            {
                instance.LookupParameter(countParams[i])?.Set(1);
            }

            // Set text parameters
            instance.LookupParameter(frontTextParams[1])?.Set(FrontSignFirst);
            if (isDouble) instance.LookupParameter(backTextParams[1])?.Set(BackSignFirst);

            if (SignRows >= 2)
            {
                instance.LookupParameter(frontTextParams[2])?.Set(FrontSignSecond);
                if (isDouble) instance.LookupParameter(backTextParams[2])?.Set(BackSignSecond);
            }
            else // Clear unused lines if rows decrease
            {
                instance.LookupParameter(frontTextParams[2])?.Set("");
                if (isDouble) instance.LookupParameter(backTextParams[2])?.Set("");
            }

            if (SignRows >= 3)
            {
                instance.LookupParameter(frontTextParams[3])?.Set(FrontSignThird);
                if (isDouble) instance.LookupParameter(backTextParams[3])?.Set(BackSignThird);
            }
            else // Clear unused lines if rows decrease
            {
                instance.LookupParameter(frontTextParams[3])?.Set("");
                if (isDouble) instance.LookupParameter(backTextParams[3])?.Set("");
            }
        }
        private string RemovePrefix(string input)
        {
            // Simplified check, assuming prefixes are always 3 characters long followed by a separator
            if (input.StartsWith("正面：") || input.StartsWith("正面:") || input.StartsWith("背面：") || input.StartsWith("背面:"))
                return input.Substring(3); // "正面：" is 3 chars, "：" is 1 char, total 4 chars. So it should be input.Substring(4) if "：" is used.
                                           // Assuming "：", ":" are treated as 1 char for simplicity or user intention, needs clarification.
                                           // If it's literally "正面", then 2 chars. Let's assume common string "正面：" has 3 visible chars, plus the separator.
                                           // More robust:
            if (input.StartsWith("正面：", StringComparison.OrdinalIgnoreCase)) return input.Substring(3); // 3 for "正面"
            if (input.StartsWith("正面:", StringComparison.OrdinalIgnoreCase)) return input.Substring(3);
            if (input.StartsWith("背面：", StringComparison.OrdinalIgnoreCase)) return input.Substring(3);
            if (input.StartsWith("背面:", StringComparison.OrdinalIgnoreCase)) return input.Substring(3);

            return input;
        }
        public ICommand PickElementCommand => new RelayCommand<GuidanceSignEntity>(PickElement);
        private void PickElement(GuidanceSignEntity entity)
        {
            if (entity == null) return;

            ExternalHandler.Run(app =>
            {
                Selection select = UIDoc.Selection;
                var currentLevelInstances = new List<ElementId>();
                currentLevelInstances.Add(entity.Id); // The tag
                currentLevelInstances.Add(entity.EntityId); // The tagged family instance
                select.SetElementIds(currentLevelInstances);
            });
        }
    }

    //public class GuidanceSignService
    //{
    //    private readonly Document _document;
    //    public GuidanceSignService(Document doc)
    //    {
    //        _document = doc;
    //    }
    //    /// <summary>
    //    /// 批量检测重复编号并回填到实体的 HasSameSerial 属性
    //    /// </summary>
    //    public void CheckForDuplicateSerials(ObservableCollection<GuidanceSignEntity> entities)
    //    {
    //        // Step 1: 按 TagName 分组
    //        var duplicateGroups = entities
    //            .GroupBy(e => e.TagName)
    //            .Where(g => g.Count() > 1)
    //            .Select(g => g.Key)
    //            .ToHashSet();

    //        // Step 2: 回填到实体
    //        foreach (var entity in entities)
    //        {
    //            entity.HasSameSerial = duplicateGroups.Contains(entity.TagName);
    //        }
    //    }
    //    /// <summary>
    //    /// 从当前文档批量构造 GuidanceSignEntity 列表
    //    /// </summary>
    //    public ObservableCollection<GuidanceSignEntity> LoadAllGuidanceSigns()
    //    {
    //        var entities = new ObservableCollection<GuidanceSignEntity>();

    //        var tags = new FilteredElementCollector(_document)
    //            .OfClass(typeof(IndependentTag))
    //            .Cast<IndependentTag>()
    //            .ToList();

    //        foreach (var tag in tags)
    //        {
    //            try
    //            {
    //                var entity = new GuidanceSignEntity(tag);
    //                entities.Add(entity);
    //            }
    //            catch
    //            {
    //                // 忽略无法解析的族实例
    //            }
    //        }
    //        // 创建完后一次性批量查重
    //        CheckForDuplicateSerials(entities);

    //        return entities;
    //    }
    //}
    //public class GuidanaceSignManagerViewModel : ObserverableObject
    //{
    //    public Document Document { get; set; }
    //    public UIDocument uIDoc { get; set; }
    //    public View ActiveView { get; set; }
    //    public UIApplication uIApp { get; set; }
    //    private readonly GuidanceSignService _service;
    //    private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
    //    public GuidanaceSignManagerViewModel(UIApplication application)
    //    {
    //        Document = application.ActiveUIDocument.Document;
    //        uIDoc = application.ActiveUIDocument;
    //        ActiveView = application.ActiveUIDocument.ActiveView;
    //        uIApp = application;
    //        _service = new GuidanceSignService(Document);
    //        QueryElement(null);
    //    }

    //    public void RefreshDuplicateCheck()
    //    {
    //        _service.CheckForDuplicateSerials(AllSigns);
    //    }
    //    public ICommand EditContentCommand => new RelayCommand<GuidanceSignEntity>(EditContent);
    //    private void EditContent(GuidanceSignEntity entity)
    //    {
    //        UniversalNewString subView = new UniversalNewString("请按规则修改标牌文字，保存更新", entity.EntityContent);
    //        if (subView.ShowDialog() != true || !(subView.DataContext is NewStringViewModel vm) || string.IsNullOrWhiteSpace(vm.NewName))
    //        {
    //            return;
    //        }
    //        if (vm.NewName == entity.EntityContent || vm.NewName == null) return;

    //        string editContent = vm.NewName;
    //        string frontContent = null;
    //        string backContent = null;
    //        if (editContent.Contains("|"))
    //        {
    //            // 分割字符串，最多分成2部分
    //            string[] parts = editContent.Split(new[] { '|' }, 2);
    //            frontContent = parts[0].Trim();
    //            backContent = parts.Length > 1 ? parts[1].Trim() : string.Empty;
    //        }
    //        else frontContent = editContent;
    //        //TaskDialog.Show("tt", frontContent);
    //        //TaskDialog.Show("tt", backContent);
    //        // 分割正面内容
    //        string[] frontParts = frontContent.Split(new[] { ';', '；' }, StringSplitOptions.RemoveEmptyEntries);
    //        int frontCount = Math.Min(frontParts.Length, 3); // 最多取3个
    //        if (frontCount > 0) FrontSignFirst = RemovePrefix(frontParts[0].Trim());
    //        if (frontCount > 1) FrontSignSecond = frontParts[1].Trim();
    //        if (frontCount > 2) FrontSignThird = frontParts[2].Trim();
    //        // 分割背面内容（如果有）
    //        int backCount = 0;
    //        if (!string.IsNullOrEmpty(backContent))
    //        {
    //            string[] backParts = backContent.Split(new[] { ';', '；' }, StringSplitOptions.RemoveEmptyEntries);
    //            backCount = Math.Min(backParts.Length, 3); // 最多取3个
    //            if (backCount > 0) BackSignFirst = RemovePrefix(backParts[0].Trim());
    //            if (backCount > 1) BackSignSecond = backParts[1].Trim();
    //            if (backCount > 2) BackSignThird = backParts[2].Trim();
    //        }
    //        else
    //        {
    //            BackSignFirst = "-";
    //            BackSignSecond = "-";
    //            BackSignThird = "-";
    //        }
    //        // 确定行数（取正反面中较大的数量）
    //        SignRows = Math.Max(frontCount, backCount);

    //        _externalHandler.Run(app =>
    //        {
    //            using (Transaction tx = new Transaction(Document, "修改标识文字"))
    //            {
    //                tx.Start();
    //                FamilyInstance instance = entity.Entity;
    //                SetSignParameters(instance, entity.IsDouble);
    //                entity.EntityContent = editContent;
    //                tx.Commit();
    //            }
    //        });
    //    }
    //    private void SetSignParameters(FamilyInstance instance, bool isDouble)
    //    {
    //        // 设置牌面数量和文字
    //        if (SignRows > 3) return;
    //        switch (SignRows)
    //        {
    //            case 1:
    //                instance.LookupParameter("推荐数量 1块").Set(1);
    //                instance.LookupParameter("推荐数量 2块").Set(0);
    //                instance.LookupParameter("推荐数量 3块").Set(0);
    //                instance.LookupParameter("文字转换").Set(FrontSignFirst);
    //                if (isDouble) instance.LookupParameter("文字转换 背面").Set(BackSignFirst);
    //                break;
    //            case 2:
    //                instance.LookupParameter("推荐数量 1块").Set(1);
    //                instance.LookupParameter("推荐数量 2块").Set(1);
    //                instance.LookupParameter("推荐数量 3块").Set(0);
    //                instance.LookupParameter("文字转换").Set(FrontSignFirst);
    //                if (isDouble) instance.LookupParameter("文字转换 背面").Set(BackSignFirst);
    //                instance.LookupParameter("文字转换 第二行").Set(FrontSignSecond);
    //                if (isDouble) instance.LookupParameter("文字转换 第二行背面").Set(BackSignSecond);
    //                break;
    //            default:
    //                instance.LookupParameter("推荐数量 1块").Set(1);
    //                instance.LookupParameter("推荐数量 2块").Set(1);
    //                instance.LookupParameter("推荐数量 3块").Set(1);
    //                instance.LookupParameter("文字转换").Set(FrontSignFirst);
    //                if (isDouble) instance.LookupParameter("文字转换 背面").Set(BackSignFirst);
    //                instance.LookupParameter("文字转换 第二行").Set(FrontSignSecond);
    //                if (isDouble) instance.LookupParameter("文字转换 第二行背面").Set(BackSignSecond);
    //                instance.LookupParameter("文字转换 第三行").Set(FrontSignThird);
    //                if (isDouble) instance.LookupParameter("文字转换 第三行背面").Set(BackSignThird);
    //                break;
    //        }
    //    }
    //    private string RemovePrefix(string input)
    //    {
    //        if (input.StartsWith("正面：") || input.StartsWith("正面:") || input.StartsWith("背面：") || input.StartsWith("背面:"))
    //            return input.Substring(3);
    //        else return input;
    //    }
    //    public int SignRows { get; set; } = 1;
    //    private string frontSignFirst = "-";
    //    private string frontSignSecond = "-";
    //    private string frontSignThird = "-";
    //    private string backSignFirst = "-";
    //    private string backSignSecond = "-";
    //    private string backSignThird = "-";
    //    public string FrontSignFirst { get => frontSignFirst; set => SetProperty(ref frontSignFirst, value); }
    //    public string FrontSignSecond { get => frontSignSecond; set => SetProperty(ref frontSignSecond, value); }
    //    public string FrontSignThird { get => frontSignThird; set => SetProperty(ref frontSignThird, value); }
    //    public string BackSignFirst { get => backSignFirst; set => SetProperty(ref backSignFirst, value); }
    //    public string BackSignSecond { get => backSignSecond; set => SetProperty(ref backSignSecond, value); }
    //    public string BackSignThird { get => backSignThird; set => SetProperty(ref backSignThird, value); }
    //    public ICommand PickElementCommand => new RelayCommand<GuidanceSignEntity>(PickElement);
    //    private void PickElement(GuidanceSignEntity entity)
    //    {
    //        _externalHandler.Run(app =>
    //        {
    //            Selection select = uIDoc.Selection;
    //            var currentLevelInstances = new List<ElementId>();
    //            currentLevelInstances.Add(entity.Id);
    //            currentLevelInstances.Add(entity.EntityId);
    //            select.SetElementIds(currentLevelInstances);
    //        });
    //    }
    //    public ICommand QueryElementCommand => new RelayCommand<string>(QueryElement);
    //    private void QueryElement(string obj)
    //    {
    //        _externalHandler.Run(app =>
    //        {
    //            AllSigns.Clear();
    //            var guidanceSigns = new FilteredElementCollector(Document).OfClass(typeof(IndependentTag)).Cast<IndependentTag>().Where(s => s.Name.StartsWith("标记_标识")).ToList();
    //            foreach (var sign in guidanceSigns)
    //            {
    //                if (string.IsNullOrEmpty(obj) || sign.TagText.Contains(obj) || sign.TagText.IndexOf(obj, StringComparison.OrdinalIgnoreCase) >= 0)
    //                {
    //                    allSigns.Add(new GuidanceSignEntity(sign));
    //                }
    //            }
    //            RefreshDuplicateCheck();
    //        });
    //    }
    //    private ObservableCollection<GuidanceSignEntity> allSigns = new ObservableCollection<GuidanceSignEntity>();
    //    public ObservableCollection<GuidanceSignEntity> AllSigns
    //    {
    //        get => allSigns;
    //        set => SetProperty(ref allSigns, value);
    //    }
    //}
}
