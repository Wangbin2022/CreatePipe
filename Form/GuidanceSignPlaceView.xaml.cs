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
        public Document Document;
        public View ActiveView;
        public UIDocument uiDoc;
        private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
        // 修复1：直接存储标记类型的 ElementId
        private ElementId _annoSymbolId = ElementId.InvalidElementId;
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
            foreach (var item in families)
            {
                if (item.Name == "标志标识 - 吊挂式") HangSignFamily = item;
                else if (item.Name == "标志标识 - 立柱式") PillarSignFamily = item;
                else if (item.Name == "标志标识 - 附着式") AttachSignFamily = item;
            }
            // 修复1：收集 FamilySymbol（族类型）而不是 IndependentTag（族实例）
            var annoSymbol = new FilteredElementCollector(Document).OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>()
                .FirstOrDefault(s => s.FamilyName.StartsWith("标记_标识") || s.Name.StartsWith("标记_标识"));
            if (annoSymbol != null) _annoSymbolId = annoSymbol.Id;

            //if (families != null)
            //{
            //    foreach (var item in families)
            //    {
            //        switch (item.Name)
            //        {
            //            case "标志标识 - 吊挂式":
            //                HangSignFamily = item;
            //                break;
            //            case "标志标识 - 立柱式":
            //                PillarSignFamily = item;
            //                break;
            //            case "标志标识 - 附着式":
            //                AttachSignFamily = item;
            //                break;
            //            default:
            //                TaskDialog.Show("tt", "No PASS");
            //                break;
            //        }
            //    }
            //}
            //var annoFamilies = new FilteredElementCollector(Document).OfClass(typeof(IndependentTag)).Cast<IndependentTag>().Where(s => s.Name.StartsWith("标记_标识")).ToList();
            //annoSignFamily = annoFamilies.FirstOrDefault();
        }
        public Family HangSignFamily { get; set; }
        public Family AttachSignFamily { get; set; }
        public Family PillarSignFamily { get; set; }
        //public IndependentTag annoSignFamily { get; set; }
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
            if (baseFamily == null || _annoSymbolId == null)
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
            if (AttachSignFamily == null || _annoSymbolId == null)
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
            selectFlowSymbol = selectCustomSymbol = selectNonFlowSymbol = null;
            foreach (var id in family.GetFamilySymbolIds())
            {
                if (Document.GetElement(id) is FamilySymbol symbol)
                {
                    if (symbol.Name.Contains("非流程")) selectNonFlowSymbol = symbol;
                    else if (symbol.Name.Contains("海关")) selectCustomSymbol = symbol;
                    else selectFlowSymbol = symbol;
                }
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
            if (LocCode == "未找到标识标记")
            {
                TaskDialog.Show("tt", "清载入标识标记族并正确设置位置编码。");
                return;
            }
            XYZ pt;
            try { pt = uiDoc.Selection.PickPoint("请点选放置位置"); }
            catch (OperationCanceledException) { return; }

            var level = Document.GetElement(ActiveView.GenLevel.Id) as Level;
            _externalHandler.Run(app =>
            {
                NewTransaction.Execute(Document, "放置标识实例", () =>
                {
                    var instance = Document.Create.NewFamilyInstance(pt, symbol, level, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                    var tag = IndependentTag.Create(Document, _annoSymbolId, ActiveView.Id, new Reference(instance), false, TagOrientation.Horizontal, pt);
                    SetSignParameters(instance, isDouble, isHang);
                    // 旋转
                    // 旋转
                    if (SignAngle != 0)
                    {
                        var axis = Line.CreateBound(pt, pt + XYZ.BasisZ);
                        instance.Location.Rotate(axis, SignAngle * Math.PI / 180.0);
                        if (SignAngle == 90 || SignAngle == 270) tag.TagOrientation = TagOrientation.Vertical;
                    }
                    instance.Symbol.LookupParameter("位置编码").Set(LocCode);
                    instance.LookupParameter("层高编码").Set(LevelCode);
                    instance.Symbol.LookupParameter("性质编码").Set(FlowCode);
                    instance.LookupParameter("本层编号").Set(SignNum);

                    if (int.TryParse(SignNum, out int parsed))
                        SignNum = (parsed + 1).ToString("D3");
                });
            });
        }
        private void SetSignParameters(FamilyInstance instance, bool isDouble, bool isHang)
        {
            // 1. 定义参数名称和对应的数据源
            var qtyNames = new[] { "推荐数量 1块", "推荐数量 2块", "推荐数量 3块" };
            var frontNames = new[] { "文字转换", "文字转换 第二行", "文字转换 第三行" };
            var backNames = new[] { "文字转换 背面", "文字转换 第二行背面", "文字转换 第三行背面" };

            var frontValues = new[] { FrontSignFirst, FrontSignSecond, FrontSignThird };
            var backValues = new[] { BackSignFirst, BackSignSecond, BackSignThird };

            // 2. 遍历 3 行，显式设置每一行的状态
            for (int i = 0; i < 3; i++)
            {
                // 索引 i 从 0 到 2
                // 如果当前索引小于 SignRows，则激活（设为1和对应文字），否则关闭（设为0和"-"）
                bool isRowActive = i < SignRows;

                // 设置数量开关 (1 或 0)
                SetParameterSafe(instance, qtyNames[i], isRowActive ? 1 : 0);

                // 设置正面文字 (内容 或 "-")
                SetParameterSafe(instance, frontNames[i], isRowActive ? frontValues[i] : "-");

                // 设置背面文字 (如果是双面且行活跃则设内容，否则设 "-")
                if (isDouble && isRowActive)
                    SetParameterSafe(instance, backNames[i], backValues[i]);
                else
                    SetParameterSafe(instance, backNames[i], "-");
            }

            // 3. 设置几何参数
            SetParameterSafe(instance, "推荐长度", SignLength / 304.8);
            SetParameterSafe(instance, "推荐宽度", SignWidth / 304.8);

            if (isHang)
            {
                SetParameterSafe(instance.Symbol, "标识高度", HangSignHeight / 304.8);
            }
            else if (instance.Symbol?.Family?.Name == "标志标识 - 附着式")
            {
                SetParameterSafe(instance, "悬挂标高", AttachSignHeight / 304.8);
            }
        }
        // --- 安全参数赋值辅助方法 ---
        private void SetParameterSafe(Element elem, string paramName, object value)
        {
            var p = elem?.LookupParameter(paramName);
            if (p == null || p.IsReadOnly) return;

            if (value is int i) p.Set(i);
            else if (value is double d) p.Set(d);
            else if (value is string s) p.Set(s ?? string.Empty);
        }
        //private void SetSignParameters(FamilyInstance instance, bool isDouble, bool isHang)
        //{
        //    // 设置牌面数量和文字
        //    if (SignRows > 3) return;

        //    switch (SignRows)
        //    {
        //        case 1:
        //            instance.LookupParameter("推荐数量 1块").Set(1);
        //            instance.LookupParameter("推荐数量 2块").Set(0);
        //            instance.LookupParameter("推荐数量 3块").Set(0);
        //            instance.LookupParameter("文字转换").Set(FrontSignFirst);
        //            if (isDouble) instance.LookupParameter("文字转换 背面").Set(BackSignFirst);
        //            break;
        //        case 2:
        //            instance.LookupParameter("推荐数量 1块").Set(1);
        //            instance.LookupParameter("推荐数量 2块").Set(1);
        //            instance.LookupParameter("推荐数量 3块").Set(0);
        //            instance.LookupParameter("文字转换").Set(FrontSignFirst);
        //            if (isDouble) instance.LookupParameter("文字转换 背面").Set(BackSignFirst);
        //            instance.LookupParameter("文字转换 第二行").Set(FrontSignSecond);
        //            if (isDouble) instance.LookupParameter("文字转换 第二行背面").Set(BackSignSecond);
        //            break;
        //        default:
        //            instance.LookupParameter("推荐数量 1块").Set(1);
        //            instance.LookupParameter("推荐数量 2块").Set(1);
        //            instance.LookupParameter("推荐数量 3块").Set(1);
        //            instance.LookupParameter("文字转换").Set(FrontSignFirst);
        //            if (isDouble) instance.LookupParameter("文字转换 背面").Set(BackSignFirst);
        //            instance.LookupParameter("文字转换 第二行").Set(FrontSignSecond);
        //            if (isDouble) instance.LookupParameter("文字转换 第二行背面").Set(BackSignSecond);
        //            instance.LookupParameter("文字转换 第三行").Set(FrontSignThird);
        //            if (isDouble) instance.LookupParameter("文字转换 第三行背面").Set(BackSignThird);
        //            break;
        //    }
        //    // 设置几何
        //    instance.LookupParameter("推荐长度").Set(SignLength / 304.8);
        //    instance.LookupParameter("推荐宽度").Set(SignWidth / 304.8);
        //    if (isHang)
        //        instance.Symbol.LookupParameter("标识高度").Set(HangSignHeight / 304.8);
        //    else if (instance.Symbol.Family.Name == "标志标识 - 附着式")
        //    {
        //        instance.LookupParameter("悬挂标高").Set(AttachSignHeight / 304.8);
        //    }
        //    //暂时没有考虑立柱式的高度设置，高度设置无效
        //}
        public ICommand ChangeViewScaleCommand => new RelayCommand<string>(ChangeViewScale);
        private void ChangeViewScale(string obj)
        {
            int.TryParse(obj, out int parsed);
            _externalHandler.Run(app =>
            {
                NewTransaction.Execute(Document, "改视图比例", () =>
                {
                    if (ActiveView.Scale != parsed)
                    {
                        ActiveView.Scale = parsed;
                    }
                });
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
            set
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
            if (string.IsNullOrEmpty(obj)) return;
            FrontSignFirst = FrontSignSecond = FrontSignThird = "-";
            BackSignFirst = BackSignSecond = BackSignThird = "-";
            SignRows = 1;

            string frontContent, backContent = null;
            if (obj.Contains("|"))
            {
                // 分割字符串，最多分成2部分
                string[] parts = obj.Split(new[] { '|' }, 2);
                frontContent = parts[0].Trim();
                backContent = parts.Length > 1 ? parts[1].Trim() : string.Empty;
            }
            else frontContent = obj;

            // 分割正面内容
            var frontParts = frontContent.Split(new[] { ';', '；' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim()).Take(3).ToArray();

            if (frontParts.Length > 0) FrontSignFirst = RemovePrefix(frontParts[0]);
            if (frontParts.Length > 1) FrontSignSecond = frontParts[1];
            if (frontParts.Length > 2) FrontSignThird = frontParts[2];
            // 分割背面内容（如果有）
            int backCount = 0;
            if (!string.IsNullOrEmpty(backContent))
            {
                var backParts = backContent.Split(new[] { ';', '；' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim()).Take(3).ToArray();
                backCount = backParts.Length;
                if (backParts.Length > 0) BackSignFirst = RemovePrefix(backParts[0]);
                if (backParts.Length > 1) BackSignSecond = backParts[1];
                if (backParts.Length > 2) BackSignThird = backParts[2];
            }
            // 确定行数（取正反面中较大的数量）
            SignRows = Math.Max(frontParts.Length, backCount);
            //TaskDialog.Show("tt", SignRows.ToString());
        }
        private string RemovePrefix(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            foreach (var prefix in new[] { "正面：", "正面:", "背面：", "背面:" })
            {
                if (input.StartsWith(prefix)) return input.Substring(prefix.Length);
            }
            return input;
        }
        public int SignRows { get; set; } = 1;
        //private string frontSignFirst, frontSignSecond, frontSignThird, backSignFirst, backSignSecond, backSignThird = "-";
        private string frontSignFirst = "-", frontSignSecond = "-", frontSignThird = "-";
        private string backSignFirst = "-", backSignSecond = "-", backSignThird = "-";
        public string FrontSignFirst { get => frontSignFirst; set => SetProperty(ref frontSignFirst, value); }
        public string FrontSignSecond { get => frontSignSecond; set => SetProperty(ref frontSignSecond, value); }
        public string FrontSignThird { get => frontSignThird; set => SetProperty(ref frontSignThird, value); }
        public string BackSignFirst { get => backSignFirst; set => SetProperty(ref backSignFirst, value); }
        public string BackSignSecond { get => backSignSecond; set => SetProperty(ref backSignSecond, value); }
        public string BackSignThird { get => backSignThird; set => SetProperty(ref backSignThird, value); }
        public ICommand InputRuleCommand => new BaseBindingCommand(InputRule);
        private void InputRule(object obj)
        {
            TaskDialog.Show("输入规则", string.Join(Environment.NewLine, new[]
              {
            "输入内容规则说明：",
            "1. 正反牌面内容以竖线| 分割，最多1个",
            "2. 正反牌面内容分别以“正面：”或“背面：”开头（可省略）",
            "3. 多行牌面内容以分号 ; 区分，每面最多3行",
            "4. 输入字符串中不得包含半角逗号 ,"
        }));
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
        public string ContentText { get => contentText; set => SetProperty(ref contentText, value); }
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
