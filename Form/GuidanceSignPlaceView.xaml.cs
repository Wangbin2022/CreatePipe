using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using CreatePipe.Utils;
using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;



namespace CreatePipe.Form
{
    /// <summary>
    /// GuidanaceSignPlaceView.xaml 的交互逻辑
    /// </summary>
    public partial class GuidanceSignPlaceView : Window
    {
        public GuidanceSignPlaceView(UIApplication uiApp)
        {
            InitializeComponent();
            this.DataContext = new GuidanceSignPlaceViewModel(uiApp);
        }
        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void ContentTb_Loaded(object sender, RoutedEventArgs e)
        {
            var tb = (System.Windows.Controls.TextBox)sender;
            // 立刻刷新一次绑定，触发 ValidationRule
            tb.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)?.UpdateSource();
        }
    }
    public class GuidanceSignPlaceViewModel : ObserverableObject
    {
        public static Document Document;
        public View ActiveView;
        public UIDocument uiDoc;
        private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
        public GuidanceSignPlaceViewModel(UIApplication uiApp)
        {
            Document = uiApp.ActiveUIDocument.Document;
            ActiveView = uiApp.ActiveUIDocument.ActiveView;
            if (ActiveView.ViewType != ViewType.FloorPlan)
            {
                TaskDialog.Show("tt", "请调整到平面视图再操作本命令");
                canPlaceSign = false;
                return;
            }
            else LevelCode = ActiveView.GenLevel.Name;
            uiDoc = uiApp.ActiveUIDocument;
            LocCode = GetlocCode();
            var families = new FilteredElementCollector(Document).OfClass(typeof(Family)).Cast<Family>().Where(s => s.Name.Contains("标志标识")).ToList();
            if (families != null)
            {
                foreach (var item in families)
                {
                    switch (item.Name)
                    {
                        case "标志标识 - 吊挂式":
                            HangSignFamily = item;
                            break;
                        case "标志标识 - 立柱式":
                            PillarSignFamily = item;
                            break;
                        case "标志标识 - 附着式":
                            AttachSignFamily = item;
                            break;
                        default:
                            TaskDialog.Show("tt", "No PASS");
                            break;
                    }
                }
            }
            var annoFamilies = new FilteredElementCollector(Document).OfClass(typeof(IndependentTag)).Cast<IndependentTag>().Where(s => s.Name.StartsWith("标记_标识")).ToList();
            annoSignFamily = annoFamilies.FirstOrDefault();
        }
        public Family HangSignFamily { get; set; }
        public Family AttachSignFamily { get; set; }
        public Family PillarSignFamily { get; set; }
        public IndependentTag annoSignFamily { get; set; }
        public ICommand execPlacementCommand => new RelayCommand<object>(execPlacement);
        private void execPlacement(object parameter)
        {
            if (string.IsNullOrEmpty(ContentText))
            {
                TaskDialog.Show("tt", "标识输入内容为空，请重新输入");
                return;
            }
            if (ActiveView.ViewType != ViewType.FloorPlan)
            {
                TaskDialog.Show("tt", "请调整到平面视图再操作本命令");
                return;
            }
            bool isHang = false;
            bool isDouble = false;
            Family baseFamily = null;
            if (parameter is int)
            {
                baseFamily = HangSignFamily;
                isHang = true;
                isDouble = true;
            }
            else if (parameter is string && int.TryParse(parameter.ToString(), out _))
            {
                baseFamily = PillarSignFamily;
                isHang = false;
                isDouble = true;
            }
            else
            {
                return;
            }
            if (baseFamily == null || annoSignFamily == null)
            {
                TaskDialog.Show("tt", "未找到指定的标识族");
                return;
            }
            LoadSymbols(baseFamily);
            var symbol = GetSymbolByFlowCode();
            if (symbol == null)
            {
                TaskDialog.Show("tt", "No PASS");
                return;
            }
            PlaceSignCommon(symbol, isDouble, isHang);
        }
        public ICommand execAttachSignCommand => new RelayCommand<int>(execAttachSign);
        private void execAttachSign(int obj)
        {
            if (string.IsNullOrEmpty(ContentText))
            {
                TaskDialog.Show("tt", "标识输入内容为空，请重新输入");
                return;
            }
            if (ActiveView.ViewType != ViewType.FloorPlan)
            {
                TaskDialog.Show("tt", "请调整到平面视图再操作本命令");
                return;
            }
            if (AttachSignFamily == null || annoSignFamily == null)
            {
                TaskDialog.Show("tt", "未找到指定的标识族");
                return;
            }
            LoadSymbols(AttachSignFamily);
            var symbol = GetSymbolByFlowCode();
            if (symbol == null)
            {
                TaskDialog.Show("tt", "No PASS");
                return;
            }
            PlaceSignCommon(symbol, false, false);
        }
        public FamilySymbol selectFlowSymbol = null;
        public FamilySymbol selectCustomSymbol = null;
        public FamilySymbol selectNonFlowSymbol = null;
        private void LoadSymbols(Family family)
        {
            selectFlowSymbol = null;
            selectCustomSymbol = null;
            selectNonFlowSymbol = null;
            foreach (var id in family.GetFamilySymbolIds())
            {
                var symbol = Document.GetElement(id) as FamilySymbol;
                if (symbol.Name.Contains("非流程")) selectNonFlowSymbol = symbol;
                else if (symbol.Name.Contains("海关")) selectCustomSymbol = symbol;
                else selectFlowSymbol = symbol;
            }
        }
        private FamilySymbol GetSymbolByFlowCode()
        {
            switch (FlowCode)
            {
                case "D":
                    return selectFlowSymbol;
                case "H":
                    return selectCustomSymbol;
                default:
                    return selectNonFlowSymbol;
            }
        }
        private void PlaceSignCommon(FamilySymbol symbol, bool isDouble, bool isHang)
        {
            if (!symbol.IsActive) symbol.Activate();

            XYZ pt;
            try { pt = uiDoc.Selection.PickPoint("请点选放置位置"); }
            catch (OperationCanceledException) { return; }

            var level = Document.GetElement(ActiveView.GenLevel.Id) as Level;
            _externalHandler.Run(app =>
            {
                using (Transaction tx = new Transaction(Document, "放置标识实例"))
                {
                    tx.Start();
                    var instance = Document.Create.NewFamilyInstance(pt, symbol, level, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                    var tag = IndependentTag.Create(Document, annoSignFamily.GetTypeId(), ActiveView.Id, new Reference(instance), false, TagOrientation.Horizontal, pt);
                    SetSignParameters(instance, isDouble, isHang);
                    // 旋转
                    if (SignAngle != 0)
                    {
                        var axis = Line.CreateBound(pt, pt + XYZ.BasisZ);
                        instance.Location.Rotate(axis, SignAngle * Math.PI / 180.0);
                    }
                    if (SignAngle == 90 || SignAngle == 270)
                        tag.TagOrientation = TagOrientation.Vertical;
                    instance.Symbol.LookupParameter("位置编码").Set(LocCode);
                    instance.LookupParameter("层高编码").Set(LevelCode);
                    instance.Symbol.LookupParameter("性质编码").Set(FlowCode);
                    instance.LookupParameter("本层编号").Set(SignNum);

                    if (int.TryParse(SignNum, out int parsed))
                        SignNum = (parsed + 1).ToString("D3");
                    tx.Commit();
                }
            });
        }
        private void SetSignParameters(FamilyInstance instance, bool isDouble, bool isHang)
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
            // 设置几何
            instance.LookupParameter("推荐长度").Set(SignLength / 304.8);
            instance.LookupParameter("推荐宽度").Set(SignWidth / 304.8);
            if (isHang)
                instance.Symbol.LookupParameter("标识高度").Set(HangSignHeight / 304.8);
            else if (instance.Symbol.Family.Name == "标志标识 - 附着式")
            {
                instance.LookupParameter("悬挂标高").Set(AttachSignHeight / 304.8);
            }
            //暂时没有考虑立柱式的高度设置，高度设置无效
        }
        public ICommand ChangeViewScaleCommand => new RelayCommand<string>(ChangeViewScale);
        private void ChangeViewScale(string obj)
        {
            int.TryParse(obj, out int parsed);
            _externalHandler.Run(app =>
            {
                using (Transaction tx = new Transaction(Document, "改视图比例"))
                {
                    tx.Start();
                    if (ActiveView.Scale != parsed)
                    {
                        ActiveView.Scale = parsed;
                    }
                    tx.Commit();
                }
            });
        }
        public bool canPlaceSign { get; set; } = true;
        private string GetlocCode()
        {
            try
            {
                var guidanceSign = new FilteredElementCollector(Document).OfClass(typeof(IndependentTag)).Cast<IndependentTag>()
                    .FirstOrDefault(s => s.Name?.StartsWith("标记_标识") == true);
                if (guidanceSign == null)
                    return "未找到标识标记";
                // 获取被标记的元素
                FamilyInstance familyInstance = (FamilyInstance)Document.GetElement(guidanceSign.TaggedLocalElementId);
                // 安全获取参数值
                Parameter locParam = familyInstance.Symbol?.LookupParameter("位置编码");
                if (locParam == null || !locParam.HasValue)
                    return "未找到位置编码参数";
                string locTag = locParam.AsString();
                return string.IsNullOrEmpty(locTag) ? "位置编码为空" : locTag;
            }
            catch (Exception ex)
            {
                return $"获取位置编码失败: {ex.Message}";
            }
        }
        private string locCode;
        public string LocCode
        {
            get => locCode;
            set { locCode = value; OnPropertyChanged(); UpdateSignNum(); }
        }
        public string levelCode;
        public string LevelCode
        {
            get => levelCode;
            set { levelCode = value; OnPropertyChanged(); UpdateSignNum(); }
        }
        private string flowCode = "D";
        public string FlowCode
        {
            get => flowCode;
            set
            {
                flowCode = value;
                OnPropertyChanged();              // 通知 FlowCode 变化
                OnPropertyChanged(nameof(previewCode)); // 同时通知依赖它的属性
                UpdateSignNum();
            }
        }
        private string _selectedFlow = "D(流程信息)";
        public string SelectedFlow
        {
            get => _selectedFlow;
            set
            {
                _selectedFlow = value;
                OnPropertyChanged();
                HandleFlowChange(value);
            }
        }
        private void HandleFlowChange(string flow)
        {
            switch (flow)
            {
                case "D(流程信息)":
                    FlowCode = "D";
                    break;
                case "H(海关)":
                    FlowCode = "H";
                    break;
                case "S(非流程信息)":
                    FlowCode = "S";
                    break;
                default:
                    // 未匹配
                    break;
            }
        }
        private string _signNum = "001";
        public string SignNum
        {
            get => _signNum;
            private set
            {
                if (_signNum == value) return;
                _signNum = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(previewCode));
            }
        }
        private void UpdateSignNum()
        {
            if (string.IsNullOrWhiteSpace(LocCode) || string.IsNullOrWhiteSpace(LevelCode) || string.IsNullOrWhiteSpace(FlowCode))
            {
                SignNum = "001";
                return;
            }
            string key = $"{LocCode}|{LevelCode}|{FlowCode}";

            int exist = new FilteredElementCollector(Document).OfClass(typeof(IndependentTag)).Cast<IndependentTag>()
                .Count(tag =>
                {
                    var fi = Document.GetElement(tag.TaggedLocalElementId) as FamilyInstance;
                    if (fi == null) return false;

                    string loc = fi.Symbol.LookupParameter("位置编码")?.AsString() ?? "";
                    string lvl = fi.LookupParameter("层高编码")?.AsString() ?? "";
                    string type = fi.Symbol.LookupParameter("性质编码")?.AsString() ?? "";
                    return loc == LocCode && lvl == LevelCode && type == FlowCode;
                });
            SignNum = (exist + 1).ToString("D3");
            OnPropertyChanged(nameof(SignNum));
        }
        public string previewCode => $"编码预览：{LocCode}{LevelCode}-{FlowCode}-{SignNum}";
        public ICommand SplitContentCommand => new RelayCommand<string>(SplitContent);
        private void SplitContent(string obj)
        {
            string frontContent = null;
            string backContent = null;
            if (obj.Contains("|"))
            {
                // 分割字符串，最多分成2部分
                string[] parts = obj.Split(new[] { '|' }, 2);
                frontContent = parts[0].Trim();
                backContent = parts.Length > 1 ? parts[1].Trim() : string.Empty;
            }
            else frontContent = obj;
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
            //TaskDialog.Show("tt", SignRows.ToString());
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
        public ICommand InputRuleCommand => new BaseBindingCommand(InputRule);
        private void InputRule(object obj)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("输入内容规则说明：");
            stringBuilder.AppendLine("1.正反牌面内容以竖线 | 分割，输入|不超过1个");
            stringBuilder.AppendLine("2.正反牌面内容分别以“正面：”“背面：”开头");
            stringBuilder.AppendLine("3.多行牌面内容以分号区分，每段最多2个 ;");
            stringBuilder.AppendLine("4.输入字符串中不得包含半角逗号 ,");
            TaskDialog.Show("tt", stringBuilder.ToString());
        }

        private bool _hasError = true;   // 初始置 true，空内容即报错
        public bool HasError
        {
            get => _hasError;
            set { _hasError = value; OnPropertyChanged(nameof(isContentValidate)); }
        }
        // 供按钮 IsEnabled 绑定
        public bool isContentValidate => !HasError;
        public int PillarSignHeight { get; set; } = 2800;
        public int AttachSignHeight { get; set; } = 2800;
        public int HangSignHeight { get; set; } = 3400;
        public int SignLength { get; set; } = 3000;
        public int SignWidth { get; set; } = 350;
        public int SignAngle { get; set; } = 0;
        public string ContentText
        {
            get => contentText;
            set => SetProperty(ref contentText, value);
        }
        private string contentText;
    }
    public static class ValidationBehaviors
    {
        #region HasError 附加属性
        public static readonly DependencyProperty HasErrorProperty =
            DependencyProperty.RegisterAttached(
                "HasError", typeof(bool), typeof(ValidationBehaviors),
                new PropertyMetadata(false, OnHasErrorChanged));
        public static bool GetHasError(DependencyObject obj) =>
            (bool)obj.GetValue(HasErrorProperty);
        public static void SetHasError(DependencyObject obj, bool value) =>
            obj.SetValue(HasErrorProperty, value);
        private static void OnHasErrorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is System.Windows.Controls.TextBox tb && tb.DataContext is GuidanceSignPlaceViewModel vm)
                vm.HasError = (bool)e.NewValue;
        }
        #endregion
    }

}
