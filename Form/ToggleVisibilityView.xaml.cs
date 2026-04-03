using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using CreatePipe.Utils;
using System.Windows;
using System.Windows.Input;

namespace CreatePipe.Form
{
    /// <summary>
    /// ToggleVisibilityView.xaml 的交互逻辑
    /// </summary>
    public partial class ToggleVisibilityView : Window
    {
        // 记录当前显示了几行（默认显示1行）
        private int _visibleRowCount = 1;
        // 界面最大允许显示的行数
        private readonly int _maxRowCount = 3;
        public ToggleVisibilityView(UIApplication uiApp)
        {
            InitializeComponent();
            this.DataContext = new ToggleVisibilityViewModel(uiApp);
            // 窗体初始化时，刷新一次显示状态，隐藏多余的行
            UpdateRowsVisibility();
        }
        // 第一行最后一个按钮（加号）点击事件
        private void BtnAddRow_Click(object sender, RoutedEventArgs e)
        {
            if (_visibleRowCount < _maxRowCount)
            {
                _visibleRowCount++;
                UpdateRowsVisibility();
            }
        }
        // 第二行最后一个按钮（减号）点击事件
        private void BtnRemoveRow_Click(object sender, RoutedEventArgs e)
        {
            if (_visibleRowCount > 1)
            {
                _visibleRowCount--;
                UpdateRowsVisibility();
            }
        }
        // 核心逻辑：根据当前的行数，自动隐藏/显示控件
        private void UpdateRowsVisibility()
        {
            // 遍历 Grid 里的所有控件 (UniversialSplitButton 和 CircleImageButton)
            foreach (UIElement child in MainGrid.Children)
            {
                // 获取当前控件属于第几行 (0代表第一行, 1代表第二行...)
                int rowIndex = System.Windows.Controls.Grid.GetRow(child);

                // 如果控件所在行小于当前允许显示的行数，就显示，否则折叠隐藏
                if (rowIndex < _visibleRowCount)
                {
                    child.Visibility = System.Windows.Visibility.Visible;
                }
                else
                {
                    // Collapsed 会让控件完全消失，且不占位
                    child.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
        }
    }
    public class ToggleVisibilityViewModel : ObserverableObject
    {
        private Document _doc;
        private View activeView;
        private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
        public ToggleVisibilityViewModel(UIApplication uiApp)
        {
            _doc = uiApp.ActiveUIDocument.Document;
            activeView = uiApp.ActiveUIDocument.ActiveView;
        }
        public string TVCommandName1 { get; set; } = "建筑墙、幕墙、门、窗等立面元素显隐";
        public ICommand TVCommand1 => new BaseBindingCommand(TVControl1);
        public void TVControl1(object obj)
        {
            _externalHandler.Run(app =>
            {
                if (!CategoryVisibilityService.CanModifyViewVisibility(activeView)) return;
                NewTransaction.Execute(_doc, "开关立面元素显示", () =>
                {
                    CategoryVisibilityService.ToggleCategoriesVisibility(_doc, activeView,
                        new[] { BuiltInCategory.OST_Walls, BuiltInCategory.OST_Doors,
                        BuiltInCategory.OST_Windows, BuiltInCategory.OST_CurtainGrids,
                        BuiltInCategory.OST_CurtainWallMullions, BuiltInCategory.OST_CurtainWallPanels });
                });
            });
        }
        public string TVCommandName2 { get; set; } = "结构柱、梁、基础等元素显隐";
        public ICommand TVCommand2 => new BaseBindingCommand(TVControl2);
        public void TVControl2(object obj)
        {
            _externalHandler.Run(app =>
            {
                if (!CategoryVisibilityService.CanModifyViewVisibility(activeView)) return;
                NewTransaction.Execute(_doc, "开关结构元素显示", () =>
                {
                    CategoryVisibilityService.ToggleCategoriesVisibility(_doc, activeView,
                        new[] { BuiltInCategory.OST_StructuralColumns, BuiltInCategory.OST_StructuralFraming,
                        BuiltInCategory.OST_StructuralFoundation });
                });
            });
        }
        public string TVCommandName3 { get; set; } = "楼板、天花板等元素显隐";
        public ICommand TVCommand3 => new BaseBindingCommand(TVControl3);
        public void TVControl3(object obj)
        {
            _externalHandler.Run(app =>
            {
                if (!CategoryVisibilityService.CanModifyViewVisibility(activeView)) return;
                NewTransaction.Execute(_doc, "开关结构元素显示", () =>
                {
                    CategoryVisibilityService.ToggleCategoriesVisibility(_doc, activeView,
                        new[] { BuiltInCategory.OST_Floors, BuiltInCategory.OST_Ceilings, });
                });
            });
        }
        public string TVCommandName4 { get; set; } = "管道及附件元素显隐";
        public ICommand TVCommand4 => new BaseBindingCommand(TVControl4);
        public void TVControl4(object obj)
        {
            _externalHandler.Run(app =>
            {
                if (!CategoryVisibilityService.CanModifyViewVisibility(activeView)) return;
                NewTransaction.Execute(_doc, "开关管道及附件显示", () =>
                {
                    CategoryVisibilityService.ToggleCategoriesVisibility(_doc, activeView,
                        new[] { BuiltInCategory.OST_PipeCurves, BuiltInCategory.OST_PipeFitting,
                            BuiltInCategory.OST_PipeAccessory, BuiltInCategory.OST_PipeTags });
                });
            });
        }
        public string TVCommandName5 { get; set; } = "风管及附件元素显隐";
        public ICommand TVCommand5 => new BaseBindingCommand(TVControl5);
        public void TVControl5(object obj)
        {
            _externalHandler.Run(app =>
            {
                if (!CategoryVisibilityService.CanModifyViewVisibility(activeView)) return;
                NewTransaction.Execute(_doc, "开关风管及附件显示", () =>
                {
                    CategoryVisibilityService.ToggleCategoriesVisibility(_doc, activeView,
                        new[] { BuiltInCategory.OST_DuctCurves, BuiltInCategory.OST_DuctFitting,
                            BuiltInCategory.OST_DuctAccessory, BuiltInCategory.OST_DuctTags });
                });
            });
        }
        public string TVCommandName6 { get; set; } = "桥架线管及附件元素显隐";
        public ICommand TVCommand6 => new BaseBindingCommand(TVControl6);
        public void TVControl6(object obj)
        {
            _externalHandler.Run(app =>
            {
                if (!CategoryVisibilityService.CanModifyViewVisibility(activeView)) return;
                NewTransaction.Execute(_doc, "开关桥架线管及附件显示", () =>
                {
                    CategoryVisibilityService.ToggleCategoriesVisibility(_doc, activeView,
                        new[] { BuiltInCategory.OST_CableTray, BuiltInCategory.OST_CableTrayFitting,
                            BuiltInCategory.OST_Conduit, BuiltInCategory.OST_ConduitFitting});
                });
            });
        }
        public string TVCommandName7 { get; set; } = "各种机械、卫浴、电气柜等机电设备及喷头显隐";
        public ICommand TVCommand7 => new BaseBindingCommand(TVControl7);
        public void TVControl7(object obj)
        {
            _externalHandler.Run(app =>
            {
                if (!CategoryVisibilityService.CanModifyViewVisibility(activeView)) return;
                NewTransaction.Execute(_doc, "开关机电设备显示", () =>
                {
                    CategoryVisibilityService.ToggleCategoriesVisibility(_doc, activeView,
                        new[] { BuiltInCategory.OST_GenericModel, BuiltInCategory.OST_SpecialityEquipment,
                        BuiltInCategory.OST_MechanicalEquipment, BuiltInCategory.OST_ElectricalEquipment,
                        BuiltInCategory.OST_Sprinklers, BuiltInCategory.OST_PlumbingFixtures });
                });
            });
        }
        public string TVCommandName8 { get; set; } = "管道、风管及管件保温层显隐";
        public ICommand TVCommand8 => new BaseBindingCommand(TVControl8);
        public void TVControl8(object obj)
        {
            _externalHandler.Run(app =>
            {
                if (!CategoryVisibilityService.CanModifyViewVisibility(activeView)) return;
                NewTransaction.Execute(_doc, "开关保温层显示", () =>
                {
                    // 一键智能反转水管和风管保温层的可见性
                    CategoryVisibilityService.ToggleCategoriesVisibility(_doc, activeView,
                        new[] { BuiltInCategory.OST_PipeInsulations, BuiltInCategory.OST_DuctInsulations });

                });
            });
        }
        public string TVCommandName9 { get; set; } = "电气灯，开关，指示，警报，通讯等末端显隐";
        public ICommand TVCommand9 => new BaseBindingCommand(TVControl9);
        public void TVControl9(object obj)
        {
            _externalHandler.Run(app =>
            {
                if (!CategoryVisibilityService.CanModifyViewVisibility(activeView)) return;
                NewTransaction.Execute(_doc, "开关电末端显示", () =>
                {
                    CategoryVisibilityService.ToggleCategoriesVisibility(_doc, activeView,
                        new[] { BuiltInCategory.OST_GenericModel, BuiltInCategory.OST_FireAlarmDevices,
                        BuiltInCategory.OST_LightingFixtures, BuiltInCategory.OST_ElectricalFixtures,
                        BuiltInCategory.OST_SecurityDevices, BuiltInCategory.OST_CommunicationDevices, });

                });
            });
        }
    }
    //    // 1. 定义数据模型
    //    public class VisibilityToggleItem
    //    {
    //        public string CommandName { get; set; }
    //        public string TransactionName { get; set; }
    //        public BuiltInCategory[] Categories { get; set; }
    //    }
    //    public class ToggleVisibilityViewModel : ObserverableObject
    //    {
    //        private Document _doc;
    //        private View activeView;
    //        private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
    //        // 2. 暴露给 XAML 绑定的集合
    //        public List<VisibilityToggleItem> ToggleItems { get; set; }
    //        public ToggleVisibilityViewModel(UIApplication uiApp)
    //        {
    //            _doc = uiApp.ActiveUIDocument.Document;
    //            activeView = uiApp.ActiveUIDocument.ActiveView;
    //            // 3. 在构造函数中统一初始化所有按钮的数据
    //            ToggleItems = new List<VisibilityToggleItem>
    //        {
    //            new VisibilityToggleItem
    //            {
    //                CommandName = "各种机械、卫浴、电气柜等机电设备及喷头显隐",
    //                TransactionName = "开关机电设备显示",
    //                Categories = new[] { BuiltInCategory.OST_GenericModel, BuiltInCategory.OST_SpecialityEquipment, BuiltInCategory.OST_MechanicalEquipment, BuiltInCategory.OST_ElectricalEquipment, BuiltInCategory.OST_Sprinklers, BuiltInCategory.OST_PlumbingFixtures }
    //            },
    //            new VisibilityToggleItem
    //            {
    //                CommandName = "管道、风管及管件保温层显隐",
    //                TransactionName = "开关保温层显示",
    //                Categories = new[] { BuiltInCategory.OST_PipeInsulations, BuiltInCategory.OST_DuctInsulations }
    //            },
    //            new VisibilityToggleItem
    //            {
    //                CommandName = "电气灯，开关，指示，警报，通讯等末端显隐",
    //                TransactionName = "开关电末端显示",
    //                Categories = new[] { BuiltInCategory.OST_GenericModel, BuiltInCategory.OST_FireAlarmDevices, BuiltInCategory.OST_LightingFixtures, BuiltInCategory.OST_ElectricalFixtures, BuiltInCategory.OST_SecurityDevices, BuiltInCategory.OST_CommunicationDevices }
    //            }
    //            // 未来增加新按钮，只需在这里加一行 new VisibilityToggleItem 即可！
    //        };
    //        }
    //        // 4. 统一的命令：通过 CommandParameter 接收传进来的 VisibilityToggleItem
    //        public ICommand UniversalToggleCommand => new BaseBindingCommand(ExecuteToggle);
    //        private void ExecuteToggle(object obj)
    //        {
    //            // 转换传入的参数
    //            if (obj is VisibilityToggleItem item)
    //            {
    //                _externalHandler.Run(app =>
    //                {
    //                    if (!CategoryVisibilityService.CanModifyViewVisibility(activeView)) return;
    //                    // 使用传入的 TransactionName 和 Categories
    //                    NewTransaction.Execute(_doc, item.TransactionName, () =>
    //                    {
    //                        CategoryVisibilityService.ToggleCategoriesVisibility(_doc, activeView, item.Categories);
    //                    });
    //                });
    //            }
    //        }
    //    }
    //2. XAML 端：使用 ItemsControl 动态生成按钮
    //你原本使用 Grid.Column="0", 1, 2 硬编码排版。现在我们使用 ItemsControl 配合 UniformGrid 自动生成这些按钮，代码瞬间清爽。
    //假设你原本的代码是放在 Grid.Row="2" 里的，你现在只需要把那三个<Button> 删掉，替换成以下代码：
    //xml
    //<!-- 将 ItemsControl 放在 Grid.Row= "2"，并让它跨越所有的列（假设你有3列） -->
    //<ItemsControl Grid.Row= "2" Grid.Column= "0" Grid.ColumnSpan= "3"
    //              ItemsSource= "{Binding ToggleItems}" >
    //    < !--1.定义面板布局：使用 UniformGrid 自动将子元素按列平铺，Columns= "3" 表示每行3个按钮 -->
    //    <ItemsControl.ItemsPanel>
    //        <ItemsPanelTemplate>
    //            <UniformGrid Columns = "3" />
    //        </ ItemsPanelTemplate >
    //    </ ItemsControl.ItemsPanel >
    //    < !--2.定义每个元素的样式模板-- >
    //    < ItemsControl.ItemTemplate >
    //        < DataTemplate >
    //            < Button Margin= "6" Height= "80"
    //                    Style= "{StaticResource {x:Static ToolBar.ButtonStyleKey}}"
    //                    BorderBrush= "Black" BorderThickness= "1"
    //                    HorizontalAlignment= "Stretch"
    //                    Command= "{Binding DataContext.UniversalToggleCommand, RelativeSource={RelativeSource AncestorType=ItemsControl}}"
    //                    CommandParameter= "{Binding}" >
    //                < Button.Content >
    //                    < !--绑定模型里的 CommandName -->
    //                    <TextBlock Text = "{Binding CommandName}"
    //                               TextWrapping= "Wrap"
    //                               TextAlignment= "Center" />
    //                </ Button.Content >
    //            </ Button >
    //        </ DataTemplate >
    //    </ ItemsControl.ItemTemplate >
    //</ ItemsControl >
    //优化的核心点解释：
    //ItemsControl 机制：它会遍历你的 ToggleItems 集合，有几个元素就自动生成几个<Button>。
    //UniformGrid Columns = "3"：完美的自动网格布局，第一行满了自动换行排第二行，完全不需要手写 Grid.Column= "x"。
    //CommandParameter= "{Binding}"：最关键的一步！这会将当前生成的这个按钮对应的 VisibilityToggleItem 数据模型当作参数（obj）传给你的 ExecuteToggle 方法。
    //RelativeSource 找命令：因为在 DataTemplate 内部，绑定的上下文变成了 VisibilityToggleItem，而我们的命令在 ViewModel 里，所以需要用 RelativeSource 往外找 ItemsControl 的 DataContext。
    //结果：以后如果你想增加第 4 个、第 5 个按钮，XAML 代码一行都不用改，只需要在 C# 的 List 里多 new 一个数据项，界面上就会自动多出一个排布整齐的按钮，并且功能完美可用！
}
