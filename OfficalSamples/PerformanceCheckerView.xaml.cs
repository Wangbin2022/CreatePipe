using Autodesk.Revit.DB;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// PerformanceCheckerView.xaml 的交互逻辑
    /// </summary>
    public partial class PerformanceCheckerView : Window
    {
        public PerformanceCheckerView(PerformanceAdviser performanceAdviser, Document document)
        {
            InitializeComponent();
            var viewModel = new PerformanceCheckerViewModel(performanceAdviser, document);
            viewModel.CloseWindow = Close;

            DataContext = viewModel;
        }
    }
    /// <summary>
    /// 主窗口ViewModel - 管理规则列表和执行操作
    /// </summary>
    public class PerformanceCheckerViewModel : ObserverableObject
    {
        private readonly PerformanceAdviser _performanceAdviser;
        private readonly Document _document;
        private ObservableCollection<RuleItemViewModel> _rules;
        private bool _isExecuting;

        public ObservableCollection<RuleItemViewModel> Rules
        {
            get => _rules;
            set { _rules = value; OnPropertyChanged(); }
        }

        public bool IsExecuting
        {
            get => _isExecuting;
            set { _isExecuting = value; OnPropertyChanged(); }
        }

        public ICommand RunTestsCommand { get; }
        public ICommand SelectAllCommand { get; }
        public ICommand DeselectAllCommand { get; }
        public ICommand CancelCommand { get; }

        public PerformanceCheckerViewModel(PerformanceAdviser performanceAdviser, Document document)
        {
            _performanceAdviser = performanceAdviser;
            _document = document;

            LoadRules();

            RunTestsCommand = new BaseBindingCommand(_ => RunSelectedTests(), _ => !IsExecuting);
            SelectAllCommand = new BaseBindingCommand(_ => SelectAll(true));
            DeselectAllCommand = new BaseBindingCommand(_ => SelectAll(false));
            CancelCommand = new BaseBindingCommand(_ => CloseWindow?.Invoke());
        }

        /// <summary>
        /// 加载所有性能规则 - 使用LINQ简化
        /// </summary>
        private void LoadRules()
        {
            var allIds = _performanceAdviser.GetAllRuleIds();
            var customRuleId = FlippedDoorCheck.Id;

            Rules = new ObservableCollection<RuleItemViewModel>(
                from PerformanceAdviserRuleId ruleId in allIds
                let isOurRule = ruleId == customRuleId
                let name = _performanceAdviser.GetRuleName(ruleId)
                let description = _performanceAdviser.GetRuleDescription(ruleId)
                let isEnabled = _performanceAdviser.IsRuleEnabled(ruleId)
                select new RuleItemViewModel(ruleId, name, description, isOurRule, isEnabled)
            );
        }

        /// <summary>
        /// 全选/取消全选
        /// </summary>
        private void SelectAll(bool select)
        {
            foreach (var rule in Rules)
            {
                rule.IsSelected = select;
            }
        }

        /// <summary>
        /// 运行选中的规则
        /// </summary>
        private void RunSelectedTests()
        {
            IsExecuting = true;
            try
            {
                // 启用选中的规则，禁用来选中的规则
                foreach (var rule in Rules)
                {
                    _performanceAdviser.SetRuleEnabled(rule.RuleId, rule.IsSelected);
                }

                // 执行所有规则（因为已经通过Enabled控制）
                var result = _performanceAdviser.ExecuteAllRules(_document);

                //// 显示结果
                //if (result == PerformanceAdviserResult.Succeeded)
                //{
                //    TaskDialog.Show("完成", "性能检查已完成。");
                //}
                //else if (result == PerformanceAdviserResult.Failed)
                //{
                //    TaskDialog.Show("警告", "部分规则执行失败。");
                //}
            }
            finally
            {
                IsExecuting = false;
            }
            CloseWindow?.Invoke();
        }

        public Action CloseWindow { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// 性能规则项ViewModel - 用于列表显示和选择
    /// </summary>
    public class RuleItemViewModel : ObserverableObject
    {
        private bool _isSelected;

        public PerformanceAdviserRuleId RuleId { get; }
        public string RuleName { get; }
        public string RuleDescription { get; }
        public bool IsOurRule { get; }
        public bool IsEnabled { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public RuleItemViewModel(PerformanceAdviserRuleId ruleId, string name,
            string description, bool isOurRule, bool isEnabled)
        {
            RuleId = ruleId;
            RuleName = name;
            RuleDescription = description;
            IsOurRule = isOurRule;
            IsEnabled = isEnabled;
            _isSelected = isEnabled; // 默认使用原启用状态
        }
    }


    /// <summary>
    /// 自定义性能规则 - 检查门是否面翻转
    /// 使用C# 7.3语法：表达式体成员、nameof
    /// </summary>
    public class FlippedDoorCheck : IPerformanceAdviserRule
    {
        private List<ElementId> _flippedDoors;
        private readonly string _name = "翻转门检查";
        private readonly string _description = "检查文档中是否存在面翻转的门实例";
        private readonly FailureDefinitionId _doorWarningId;
        private readonly FailureDefinition _doorWarning;
        private static readonly PerformanceAdviserRuleId _ruleId =
            new PerformanceAdviserRuleId(new Guid("BC38854474284491BD03795675AC7386"));

        public static PerformanceAdviserRuleId Id => _ruleId;

        public FlippedDoorCheck()
        {
            _doorWarningId = new FailureDefinitionId(new Guid("25570B8FD4AD42baBD78469ED60FB9A3"));
            _doorWarning = FailureDefinition.CreateFailureDefinition(
                _doorWarningId,
                FailureSeverity.Warning,
                "文档中存在面翻转的门。");
        }

        public void InitCheck(Document document)
        {
            _flippedDoors = new List<ElementId>();
            _flippedDoors.Clear();
        }

        /// <summary>
        /// 检查单个元素 - 使用模式匹配
        /// </summary>
        public void ExecuteElementCheck(Document document, Element element)
        {
            if (element is FamilyInstance doorInstance && doorInstance.FacingFlipped)
            {
                _flippedDoors.Add(doorInstance.Id);
            }
        }

        /// <summary>
        /// 最终检查 - 汇总结果并报告
        /// </summary>
        public void FinalizeCheck(Document document)
        {
            if (_flippedDoors.Count == 0)
            {
                Debug.WriteLine("没有翻转的门。检查通过。");
                return;
            }

            // 发布警告消息
            var failureMsg = new FailureMessage(_doorWarningId);
            failureMsg.SetFailingElements(_flippedDoors);

            using (var transaction = new Transaction(document, "性能检查报告"))
            {
                transaction.Start();
                PerformanceAdviser.GetPerformanceAdviser().PostWarning(failureMsg);
                transaction.Commit();
            }

            _flippedDoors.Clear();
        }

        public string GetDescription() => _description;
        public string GetName() => _name;
        public bool WillCheckElements() => true;

        /// <summary>
        /// 返回元素过滤器 - 只检查门类别
        /// </summary>
        public ElementFilter GetElementFilter(Document document) =>
            new ElementCategoryFilter(BuiltInCategory.OST_Doors);
    }
}
