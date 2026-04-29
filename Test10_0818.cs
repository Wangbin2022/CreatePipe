using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CreatePipe.filter;
using CreatePipe.Form;
using CreatePipe.OfficalSamples;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using View = Autodesk.Revit.DB.View;

namespace CreatePipe
{
    [Transaction(TransactionMode.Manual)]
    public class Test10_0818 : IExternalCommand
    {

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
            UIApplication uiApp = commandData.Application;

            //////0429 官方代码测试




            //////0428 官方代码测试
            //CancelSave逻辑已基本实现，但内部有个LogManager可以看一下思路与自己完成的是否一致。ds改写后变动很大。

            //Revit 结构边界条件编辑器插件，没啥使用可能，跳过
            //选择结构元素（柱、梁、墙、楼板等）
            //查看 / 编辑边界条件属性（固定、铰支、滚动支座、自定义）
            //支持三种边界条件类型：点、线、面
            //设置弹簧刚度（用户自定义状态）

            //BRepBuilder没啥使用可能，跳过

            //基本可用，难得
            ////Revit插件，用于浏览和查看文档中的参数绑定关系：
            ////获取参数绑定 - 从文档的ParameterBindings中获取所有参数绑定
            ////树形展示 - 以树形结构显示参数名称及其绑定的类别
            ////参数分类 - 展示每个参数绑定到哪些构件类别（如梁、楼板等）
            //try
            //{
            //    // 创建视图模型
            //    var viewModel = new ParameterBindingBrowserViewModel(commandData);
            //    // 创建并显示窗口
            //    var window = new ParameterBindingBrowserWindow
            //    {
            //        DataContext = viewModel
            //    };
            //    // 设置关闭回调
            //    viewModel.CloseAction = window.Close;
            //    // 显示窗口（非模态）
            //    window.Show();
            //    return Result.Succeeded;
            //}
            //catch (Exception ex)
            //{
            //    message = ex.Message;
            //    return Result.Failed;
            //}

            ////插件是否必要？
            ////翻改逻辑有问题，builtInParameter需单独处理
            ////Revit插件，用于为梁和楼板添加共享参数并管理唯一标识：
            ////添加共享参数 - 创建"Unique ID"共享参数，绑定到梁和楼板类别
            ////自动赋值 - 为每个构件生成GUID并写入参数
            ////显示参数值 - 在列表中显示选中构件的唯一ID
            ////查找定位 - 根据唯一ID在模型中定位构件
            //using (var transaction = new Transaction(commandData.Application.ActiveUIDocument.Document, "唯一ID管理"))
            //{
            //    transaction.Start();

            //    var viewModel = new UniqueIdManagerViewModel(commandData);
            //    var window = new UniqueIdManagerWindow { DataContext = viewModel };
            //    viewModel.CloseAction = window.Close;
            //    window.ShowDialog();
            //    transaction.Commit();
            //    return Result.Succeeded;
            //}

            ////默认为自动执行，需要调整
            ////需要看一下早期叶的几何计算处理相关视频,需要学习实现使用的方法
            ////Revit插件，用于自动解决管道与结构构件（梁、风管等）的碰撞问题：
            ////碰撞检测 - 使用ReferenceIntersector检测管道与周围构件的交叉
            ////过滤碰撞对象 - 只关注管道、风管、结构框架
            ////分段处理 - 将碰撞区域分段，生成U形绕行路径
            ////自动绕行 - 创建偏移管道和弯头绕过障碍物
            //using (var transaction = new Transaction(commandData.Application.ActiveUIDocument.Document, "解决管道碰撞"))
            //{
            //    try
            //    {
            //        transaction.Start();
            //        var resolver = new PipeCollisionResolver(commandData);
            //        var resolvedCount = resolver.ResolveAllPipes();
            //        transaction.Commit();
            //        TaskDialog.Show("完成", $"共处理了 {resolvedCount} 条管道的碰撞问题");
            //        return Result.Succeeded;
            //    }
            //    catch (Exception ex)
            //    {
            //        transaction.RollBack();
            //        message = ex.Message;
            //        return Result.Failed;
            //    }
            //}

            //AutoUpdate功能已实现在记录工作时间
            //Revit外部应用程序，用于监控文档打开事件并自动修改项目信息：
            //事件注册 - 注册DocumentOpened事件
            //日志记录 - 记录事件参数和操作结果
            //自动修改 - 当文档打开时自动修改项目地址信息
            //异常处理 - 跳过族文档，处理修改失败的情况

            ////可以借鉴RoomEntity的思路，加标记相比系统级别自动没想出太多优点。
            ////Revit插件，用于自动为房间添加标记：
            ////选择楼层 - 选择需要添加房间标记的楼层
            ////选择标记类型 - 选择使用的房间标记类型
            ////显示房间列表 - 展示该楼层所有房间及已有标记数量
            ////自动标记 - 为所有未标记房间自动添加标记
            //var viewModel = new AutoTagRoomsViewModel(commandData);
            //var window = new AutoTagRoomsWindow { DataContext = viewModel };
            //viewModel.CloseAction = window.Close;
            //window.ShowDialog();

            ///比较复杂，没啥实用性，只实现了界面
            ////Revit外部应用程序，用于监控视图打印事件并在打印时添加水印：
            ////事件注册 - 注册ViewPrinting和ViewPrinted事件
            ////打印前处理 - 在视图上创建带打印信息的文字注释
            ////打印后清理 - 删除添加的文字注释
            ////日志记录 - 记录事件参数和打印信息到日志文件
            //// 创建监控窗口（可选，显示在Revit中）
            //var viewModel = new PrintMonitorViewModel();
            //var window = new PrintMonitorWindow { DataContext = viewModel };
            //window.ShowDialog();
            //return Result.Succeeded;

            ////有点意思的功能，可以考虑细化按区域布置，风管尺寸变径、分叉如何考虑，要改为非模态调用
            ////Revit插件，用于自动创建空调风管系统：
            ////连接设备 - 将1个送风设备和2个末端设备用风管连接
            ////智能路径规划 - 自动计算最优的风管布置路径
            ////创建管件 - 自动添加弯头、三通等管件
            ////系统日志 - 记录创建的风管系统详细信息
            //var viewModel = new DuctSystemViewModel(commandData);
            //var window = new DuctSystemWindow { DataContext = viewModel };
            //viewModel.CloseAction = window.Close;
            //window.ShowDialog();

            ////Revit插件，用于批量向族文件添加参数：
            ////单文件模式 - 向当前打开的族文档添加参数
            ////批量模式 - 遍历文件夹中的所有族文件，批量添加参数
            ////参数来源 - 从文本文件读取参数定义（支持族参数和共享参数）
            ////参数格式 - 名称、分组、类型、实例 / 类型参数
            //var viewModel = new AddParameterViewModel(commandData);
            //var window = new AddParameterWindow { DataContext = viewModel };
            //viewModel.CloseAction = window.Close;
            //window.ShowDialog();
            ////// 批量模式入口（可复用同一个ViewModel，通过参数区分）
            ////[Transaction(TransactionMode.Manual)]
            ////    public class AddParameterToFamilies : IExternalCommand
            ////{
            ////    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
            ////    {
            ////        var viewModel = new AddParameterViewModel(commandData);
            ////        viewModel.IsBatchMode = true;
            ////        var window = new AddParameterWindow { DataContext = viewModel };
            ////        viewModel.CloseAction = window.Close;
            ////        window.ShowDialog();
            ////        return Result.Succeeded;
            ////    }
            ////}

            ////界面和代码完整，但似乎没有实际执行，选择后未识别到符合条件的构件
            ////这是一个Revit插件，用于自动连接重叠的几何形体（如体量、通用模型）：
            ////检测重叠 - 通过几何相交测试判断两个实体是否重叠
            ////查找重叠组 - 递归查找所有相互重叠的构件集合
            ////合并几何 - 使用CombineElements将重叠的几何合并为一个整体
            ////两种模式 - 支持选中构件合并或全文档自动合并
            //var viewModel = new AutoJoinViewModel(commandData);
            //var window = new AutoJoinWindow { DataContext = viewModel };
            //viewModel.CloseAction = window.Close;
            //window.ShowDialog();

            ///WPF xaml语法存在问题
            ///Revit插件，用于编辑面积配筋(AreaReinforcement)的参数：
            //支持两种构件类型 - 墙体面积配筋(Wall)和楼板面积配筋(Floor)
            //参数分类管理 - 按图层分类（外部 / 内部、顶部 / 底部、主向 / 次向）
            //动态数据源 - 从当前项目获取钢筋类型和弯钩类型列表
            //属性网格编辑 - 使用PropertyGrid控件进行参数编辑
            //var selectedIds = commandData.Application.ActiveUIDocument.Selection.GetElementIds();
            //// 验证是否选中构件
            //if (selectedIds.Count != 1)
            //{
            //    message = "请只选择一个面积配筋构件";
            //    return Result.Failed;
            //}
            //var element = doc.GetElement(selectedIds.First());
            //var areaRein = element as AreaReinforcement;
            //if (areaRein == null)
            //{
            //    message = "请选择一个面积配筋构件";
            //    return Result.Failed;
            //}
            //var service = new AreaReinforcementParameterService(commandData);
            //// 验证项目中是否有钢筋类型和弯钩类型
            //if (!service.HasRequiredTypes)
            //{
            //    message = "当前项目中缺少钢筋类型或弯钩类型定义";
            //    return Result.Failed;
            //}
            //var viewModel = new AreaReinforcementParameterViewModel(commandData, areaRein);
            //var window = new AreaReinforcementParameterWindow { DataContext = viewModel };
            //viewModel.CloseAction = window.Close;
            //window.ShowDialog();

            ////Revit插件，用于处理面积配筋(AreaReinforcement)的显示和弯钩设置：
            ////验证选择 - 检查用户是否只选择了一个矩形面积配筋
            ////关闭图层 - 关闭除主方向层以外的所有钢筋层
            ////移除弯钩 - 移除主要方向边界曲线上的弯钩
            ////显示结果 - 通过对话框告知用户操作结果
            //var selectedIds = commandData.Application.ActiveUIDocument.Selection.GetElementIds().ToList();
            //// 验证是否选中了构件
            //if (!selectedIds.Any())
            //{
            //    TaskDialog.Show("提示", "请至少选择一个面积配筋");
            //    return Result.Cancelled;
            //}
            //// 创建视图模型
            //var viewModel = new AreaReinforcementViewModel(commandData, selectedIds);
            //var window = new AreaReinforcementWindow { DataContext = viewModel };
            //viewModel.CloseAction = window.Close;
            //window.ShowDialog();
            //// 显示操作结果
            //if (viewModel.OperationSucceeded)
            //{
            //    TaskDialog.Show("成功", viewModel.ResultMessage);
            //    return Result.Succeeded;
            //}
            //message = viewModel.ErrorMessage;

            /// 载入构件存在崩溃问题，未涉及到导出环境
            ////VB转Revit插件，用于将项目中所有构件按类别分组导出到Excel：
            ////收集所有非类型构件 - 过滤掉ElementType，只保留实例构件
            ////按Category分组 - 将相同类别的构件归类
            ////提取公共属性 - 找出同类构件共有的参数
            ////导出到Excel - 每个类别创建一个工作表，列出构件ID和所有公共参数值
            //try
            //{
            //    // 创建视图模型并传入当前文档
            //    var viewModel = new ExportToExcelViewModel(commandData, doc);
            //    var window = new ExportToExcelWindow { DataContext = viewModel };
            //    // 设置关闭回调
            //    viewModel.CloseAction = window.Close;
            //    window.ShowDialog();
            //    return Result.Succeeded;
            //}
            //catch (Exception ex)
            //{
            //    message = ex.Message;
            //    return Result.Failed;
            //}

            ////Revit插件，用于显示选中构件的分析模型支撑信息：
            ////获取选中构件 - 从Revit当前选择集中获取所有构件
            ////提取分析模型 - 获取每个构件的AnalyticalModel
            ////判断支撑状态 - 通过IsElementFullySupported()判断是否完全支撑
            ////获取支撑类型 - 调用GetAnalyticalModelSupports()获取支撑详细信息
            ////分类显示 - 在表格中显示构件ID、类型名称、支撑类型、备注说明
            //// 获取当前选中的构件ID集合
            //List<ElementId> selectedIds = commandData.Application.ActiveUIDocument.Selection.GetElementIds().ToList();
            //// 如果没有选中任何构件，提示用户
            //if (!selectedIds.Any())
            //{
            //    TaskDialog.Show("提示", "请至少选择一个构件");
            //    return Result.Cancelled;
            //}
            //// 创建视图模型并传入选中的构件数据
            //var viewModel = new AnalyticalSupportViewModel(commandData, selectedIds);
            //var window = new AnalyticalSupportWindow { DataContext = viewModel };
            //window.ShowDialog();

            ////0427 官方代码测试
            ////Revit插件，用于管理空间(Space)和区域(Zone)，主要功能包括：
            ////切换楼层(Level) - 不同楼层显示不同的空间和区域
            ////创建空间(Create Spaces) - 在当前楼层自动创建所有封闭区域的空间
            ////创建区域(Create Zone) - 在当前楼层创建新区域
            ////编辑区域(Edit Zone) - 将空间添加到区域或从区域移除
            //using (var trans = new Transaction(doc, "AddSpaceAndZone"))
            //{
            //    trans.Start();
            //    var viewModel = new ZoneEditorMainViewModel(commandData);
            //    var window = new ZoneEditorMainWindow { DataContext = viewModel };
            //    var result = window.ShowDialog() == true;
            //    if (result) trans.Commit();
            //    else trans.RollBack();
            //    return result ? Result.Succeeded : Result.Cancelled;
            //}
            ////读取视图放到图纸上
            //var viewModel = new MainViewModel(doc);
            //var window = new AllViewsWindow { DataContext = viewModel };
            //return window.ShowDialog() == true ? Result.Succeeded : Result.Cancelled;

            return Result.Succeeded;
        }

    }
}
