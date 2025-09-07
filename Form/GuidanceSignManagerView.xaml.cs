using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CreatePipe.cmd;
using CreatePipe.models;
using CreatePipe.Utils;
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
    public class GuidanceSignService
    {
        private readonly Document _document;
        public GuidanceSignService(Document doc)
        {
            _document = doc;
        }
        /// <summary>
        /// 批量检测重复编号并回填到实体的 HasSameSerial 属性
        /// </summary>
        public void CheckForDuplicateSerials(ObservableCollection<GuidanceSignEntity> entities)
        {
            // Step 1: 按 TagName 分组
            var duplicateGroups = entities
                .GroupBy(e => e.TagName)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToHashSet();

            // Step 2: 回填到实体
            foreach (var entity in entities)
            {
                entity.HasSameSerial = duplicateGroups.Contains(entity.TagName);
            }
        }
        /// <summary>
        /// 从当前文档批量构造 GuidanceSignEntity 列表
        /// </summary>
        public ObservableCollection<GuidanceSignEntity> LoadAllGuidanceSigns()
        {
            var entities = new ObservableCollection<GuidanceSignEntity>();

            var tags = new FilteredElementCollector(_document)
                .OfClass(typeof(IndependentTag))
                .Cast<IndependentTag>()
                .ToList();

            foreach (var tag in tags)
            {
                try
                {
                    var entity = new GuidanceSignEntity(tag);
                    entities.Add(entity);
                }
                catch
                {
                    // 忽略无法解析的族实例
                }
            }
            // 创建完后一次性批量查重
            CheckForDuplicateSerials(entities);

            return entities;
        }
    }
    public class GuidanaceSignManagerViewModel : ObserverableObject
    {
        public Document Document { get; set; }
        public UIDocument uIDoc { get; set; }
        public View ActiveView { get; set; }
        public UIApplication uIApp { get; set; }
        private readonly GuidanceSignService _service;
        private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
        public GuidanaceSignManagerViewModel(UIApplication application)
        {
            Document = application.ActiveUIDocument.Document;
            uIDoc = application.ActiveUIDocument;
            ActiveView = application.ActiveUIDocument.ActiveView;
            uIApp = application;
            _service = new GuidanceSignService(Document);
            QueryElement(null);
        }

        public void RefreshDuplicateCheck()
        {
            _service.CheckForDuplicateSerials(AllSigns);
        }
        public ICommand EditContentCommand => new RelayCommand<GuidanceSignEntity>(EditContent);
        private void EditContent(GuidanceSignEntity entity)
        {
            UniversalNewString subView = new UniversalNewString("请按规则修改标牌文字，保存更新", entity.EntityContent);
            if (subView.ShowDialog() != true || !(subView.DataContext is NewStringViewModel vm) || string.IsNullOrWhiteSpace(vm.NewName))
            {
                return;
            }
            if (vm.NewName == entity.EntityContent || vm.NewName == null) return;

            string editContent = vm.NewName;
            string frontContent = null;
            string backContent = null;
            if (editContent.Contains("|"))
            {
                // 分割字符串，最多分成2部分
                string[] parts = editContent.Split(new[] { '|' }, 2);
                frontContent = parts[0].Trim();
                backContent = parts.Length > 1 ? parts[1].Trim() : string.Empty;
            }
            else frontContent = editContent;
            //TaskDialog.Show("tt", frontContent);
            //TaskDialog.Show("tt", backContent);
            // 分割正面内容
            string[] frontParts = frontContent.Split(new[] { ';', '；' }, StringSplitOptions.RemoveEmptyEntries);
            int frontCount = Math.Min(frontParts.Length, 3); // 最多取3个
            if (frontCount > 0) FrontSignFirst = RemovePrefix(frontParts[0].Trim());
            if (frontCount > 1) FrontSignSecond = frontParts[1].Trim();
            if (frontCount > 2) FrontSignThird = frontParts[2].Trim();
            // 分割背面内容（如果有）
            int backCount = 0;
            if (!string.IsNullOrEmpty(backContent))
            {
                string[] backParts = backContent.Split(new[] { ';', '；' }, StringSplitOptions.RemoveEmptyEntries);
                backCount = Math.Min(backParts.Length, 3); // 最多取3个
                if (backCount > 0) BackSignFirst = RemovePrefix(backParts[0].Trim());
                if (backCount > 1) BackSignSecond = backParts[1].Trim();
                if (backCount > 2) BackSignThird = backParts[2].Trim();
            }
            else
            {
                BackSignFirst = "-";
                BackSignSecond = "-";
                BackSignThird = "-";
            }
            // 确定行数（取正反面中较大的数量）
            SignRows = Math.Max(frontCount, backCount);

            _externalHandler.Run(app =>
            {
                using (Transaction tx = new Transaction(Document, "修改标识文字"))
                {
                    tx.Start();
                    FamilyInstance instance = entity.Entity;
                    SetSignParameters(instance, entity.IsDouble);
                    entity.EntityContent = editContent;
                    tx.Commit();
                }
            });
        }
        private void SetSignParameters(FamilyInstance instance, bool isDouble)
        {
            // 设置牌面数量和文字
            if (SignRows > 3) return;
            switch (SignRows)
            {
                case 1:
                    instance.LookupParameter("推荐数量 1块").Set(1);
                    instance.LookupParameter("推荐数量 2块").Set(0);
                    instance.LookupParameter("推荐数量 3块").Set(0);
                    instance.LookupParameter("文字转换").Set(FrontSignFirst);
                    if (isDouble) instance.LookupParameter("文字转换 背面").Set(BackSignFirst);
                    break;
                case 2:
                    instance.LookupParameter("推荐数量 1块").Set(1);
                    instance.LookupParameter("推荐数量 2块").Set(1);
                    instance.LookupParameter("推荐数量 3块").Set(0);
                    instance.LookupParameter("文字转换").Set(FrontSignFirst);
                    if (isDouble) instance.LookupParameter("文字转换 背面").Set(BackSignFirst);
                    instance.LookupParameter("文字转换 第二行").Set(FrontSignSecond);
                    if (isDouble) instance.LookupParameter("文字转换 第二行背面").Set(BackSignSecond);
                    break;
                default:
                    instance.LookupParameter("推荐数量 1块").Set(1);
                    instance.LookupParameter("推荐数量 2块").Set(1);
                    instance.LookupParameter("推荐数量 3块").Set(1);
                    instance.LookupParameter("文字转换").Set(FrontSignFirst);
                    if (isDouble) instance.LookupParameter("文字转换 背面").Set(BackSignFirst);
                    instance.LookupParameter("文字转换 第二行").Set(FrontSignSecond);
                    if (isDouble) instance.LookupParameter("文字转换 第二行背面").Set(BackSignSecond);
                    instance.LookupParameter("文字转换 第三行").Set(FrontSignThird);
                    if (isDouble) instance.LookupParameter("文字转换 第三行背面").Set(BackSignThird);
                    break;
            }
        }
        private string RemovePrefix(string input)
        {
            if (input.StartsWith("正面：") || input.StartsWith("正面:") || input.StartsWith("背面：") || input.StartsWith("背面:"))
                return input.Substring(3);
            else return input;
        }
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
        public ICommand PickElementCommand => new RelayCommand<GuidanceSignEntity>(PickElement);
        private void PickElement(GuidanceSignEntity entity)
        {
            _externalHandler.Run(app =>
            {
                Selection select = uIDoc.Selection;
                var currentLevelInstances = new List<ElementId>();
                currentLevelInstances.Add(entity.Id);
                currentLevelInstances.Add(entity.EntityId);
                select.SetElementIds(currentLevelInstances);
            });
        }
        public ICommand QueryElementCommand => new RelayCommand<string>(QueryElement);
        private void QueryElement(string obj)
        {
            _externalHandler.Run(app =>
            {
                AllSigns.Clear();
                var guidanceSigns = new FilteredElementCollector(Document).OfClass(typeof(IndependentTag)).Cast<IndependentTag>().Where(s => s.Name.StartsWith("标记_标识")).ToList();
                foreach (var sign in guidanceSigns)
                {
                    if (string.IsNullOrEmpty(obj) || sign.TagText.Contains(obj) || sign.TagText.IndexOf(obj, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        allSigns.Add(new GuidanceSignEntity(sign));
                    }
                }
                RefreshDuplicateCheck();
            });
        }
        private ObservableCollection<GuidanceSignEntity> allSigns = new ObservableCollection<GuidanceSignEntity>();
        public ObservableCollection<GuidanceSignEntity> AllSigns
        {
            get => allSigns;
            set => SetProperty(ref allSigns, value);
        }
    }
}
