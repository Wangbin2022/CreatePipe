using Autodesk.Revit.Attributes;
using Autodesk.Revit.Creation;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.DB.Fabrication;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CreatePipe.filter;
using CreatePipe.Form;
using CreatePipe.OfficalSamples;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using static CreatePipe.OfficalSamples.DuplicateView;
using View = Autodesk.Revit.DB.View;
//ObserverableObject
//service.Update(++index, id.Value.ToString());
//set => SetProperty(ref _maximum, value);
// string message = string.Empty;

namespace CreatePipe
{
    [Transaction(TransactionMode.Manual)]
    public class Test10_0818 : IExternalCommand
    {

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Autodesk.Revit.DB.Document doc = uiDoc.Document;
            Autodesk.Revit.DB.View activeView = uiDoc.ActiveView;
            UIApplication uiApp = commandData.Application;

            //////0502 官方代码测试
            //Loads 官方荷载管理 跳过

            ////LevelsProperty DisplayUnitType有问题
            ////Revit标高管理工具，用于查看、修改、添加和删除Revit项目中的标高。核心功能
            ////显示所有标高 - 在DataGridView中显示标高的名称和高度
            ////编辑标高 - 支持直接修改标高名称和高度
            ////添加标高 - 新增自定义标高
            ////删除标高 - 删除选中的标高
            ////单位转换 - 支持Revit内部单位与显示单位的转换
            //try
            //{
            //    var window = new LevelsPropertyView(uiApp);
            //    var result = window.ShowDialog();
            //    return result == true ? Result.Succeeded : Result.Cancelled;
            //}
            //catch (Exception ex)
            //{
            //    message = ex.Message;
            //    return Result.Failed;
            //} 

            ////Journaling 官方日志功能实现
            ////Revit日志记录与重放工具，演示如何使用Revit的JournalData功能保存和恢复用户操作。核心功能
            ////首次运行 - 显示对话框收集墙体创建参数（类型、标高、起点、终点）
            ////创建墙体 - 根据用户输入创建墙体
            ////保存到日志 - 将参数保存到Revit日志中（墙体类型名称、标高ID、起点 / 终点坐标）
            ////重放操作 - 再次运行时从日志读取参数，自动创建相同墙体
            ////无需用户交互 - 有日志数据时直接创建，实现操作自动化
            //try
            //{
            //    if (commandData.Application.ActiveUIDocument?.Document == null)
            //    {
            //        message = "请先打开一个Revit项目文档";
            //        return Result.Failed;
            //    }

            //    var window = new JournalingView(commandData);
            //    var result = window.ShowDialog();

            //    return result == true ? Result.Succeeded : Result.Cancelled;
            //}
            //catch (Exception ex)
            //{
            //    message = ex.Message;
            //    return Result.Failed;
            //}

            ////InvisibleParam 
            ////Revit共享参数创建工具，演示如何通过参数文件创建可见和不可见的共享参数。
            //new InvisibleParam(commandData);

            ////InPlaceFamilyAnalyzer 附加多余内容太多，DataGrid绑定有问题 待深化
            ////Revit内建族（In - Place Family）分析工具，用于查看内建族实例的属性并可视化其分析模型。核心功能
            ////选择内建族实例 - 用户选择一个具有分析模型的内建族
            ////显示属性 - 在PropertyGrid中显示实例的属性（ID、名称、族类型、结构类型等）
            ////3D模型可视化 - 在PictureBox中显示分析模型的3D轮廓，支持旋转交互
            ////矩阵变换 - 实现3D到2D的投影和旋转变换
            ////技术架构
            ////使用AnalyticalModel.GetCurves()获取分析模型的曲线
            ////实现3D矩阵变换（绕X / Y / Z轴旋转）
            ////自定义PictureBox3D控件处理鼠标交互
            //try
            //{
            //    var window = new InPlaceFamilyAnalyzerView(uiApp);
            //    var result = window.ShowDialog();
            //    return result == true ? Result.Succeeded : Result.Cancelled;
            //}
            //catch (Exception ex)
            //{
            //    message = ex.Message;
            //    return Result.Failed;
            //}

            //ImportExport 已基本实现了，需要的时候再看吧
            //视图相关格式：DWG、DXF、SAT、DWF、DGN、Image 使用 ExportWithViewsForm（需要选择视图）
            //Civil3D格式：使用专用 ExportCivil3DForm 并验证数据有效性
            //简单格式：GBXML、FBX 直接使用 SaveFileDialog 选择路径

            //CreateOrthogonalGrid 仅转正交轴网生成逻辑
            ////GridCreation有三种方法 提取公共Validation验证类还是用了WinForm待替换
            ////建立正交轴网
            ////建立弧线放射轴网
            ////基于选择线建立轴网
            ////正交轴网生成代码
            //try
            //{
            //    // 显示WPF窗口
            //    var window = new CreateOrthogonalGridView(uiApp);
            //    var result = window.ShowDialog();
            //    //var result = window.ShowModal();
            //    return result == true ? Result.Succeeded : Result.Cancelled;
            //}
            //catch (System.Exception ex)
            //{
            //    message = ex.Message;
            //    return Result.Failed;
            //}

            //RevitDefaultFamilyTypes  Revit默认族类型管理器，用于查看和修改Revit中各种族类别的默认类型设置
            //列出所有族类别（门窗、家具、照明设备等）
            //显示每个类别的有效族类型
            //查看和修改默认族类型
            //支持特殊类别：MatchModel和MatchDetail（组件类型）

            //RevitDefaultElementTypes 定义并非一般Window而是page，暂不深究实现
            //Revit默认元素类型管理器，用于查看和修改Revit中各种元素类型的默认设置。核心功能
            //显示所有元素类型组（如墙类型、楼板类型、尺寸标注类型等）
            //列出每个组的有效候选类型
            //查看当前的默认类型
            //修改默认类型（通过下拉选择）
            //try
            //{
            //    if (uiDoc?.Document == null)
            //    {
            //        message = "没有活动的文档";
            //        return Result.Failed;
            //    }
            //    // 创建视图模型
            //    var viewModel = new DefaultElementTypesViewModel(uiApp);
            //    viewModel.SetDocument(uiDoc.Document);
            //    // 创建视图
            //    var view = new DefaultElementTypesView();
            //    view.SetViewModel(viewModel);
            //    // 注册并显示可停靠面板
            //    var paneId = DefaultElementTypesView.PaneId;
            //    var pane = uiApp.GetDockablePane(paneId);
            //    if (pane == null)
            //    {
            //        uiApp.RegisterDockablePane(paneId, "默认元素类型管理器", view);
            //        pane = uiApp.GetDockablePane(paneId);
            //    }
            //    else
            //    {
            //        // 更新现有面板内容
            //        //pane.Close();
            //        uiApp.RegisterDockablePane(paneId, "默认元素类型管理器", view);
            //        pane = uiApp.GetDockablePane(paneId);
            //    }
            //    // 显示面板
            //    pane.Show();
            //    return Result.Succeeded;
            //}
            //catch (Exception ex)
            //{
            //    message = ex.Message;
            //    return Result.Failed;
            //}

            ////GeometryCreation_BooleanOperation 几何布尔运算？？RevitCSGGenerator
            ////Revit CSG（Constructive Solid Geometry，构造实体几何）工具，用于创建和显示布尔运算后的3D实体。核心功能
            ////创建5个基本几何体：立方体、球体、沿X / Y / Z轴的圆柱体
            ////执行布尔运算：并集、交集、差集
            ////构建CSG树：按照特定顺序组合几何体
            ////可视化显示：在Revit中创建3D视图并着色显示结果
            //new RevitCSGGenerator(commandData);

            ////GenericStructuralConnection
            ////Revit结构连接管理插件，提供以下功能：通用结构连接操作：
            ////创建：选择结构构件创建通用连接
            ////删除：删除选中的连接
            ////读取：显示连接信息（ID、类型、连接的构件ID）
            ////更新：向现有连接添加更多构件
            ////详细结构连接操作：
            ////创建：创建特定类型的详细连接（如US夹持角钢）
            ////更改：更改连接类型（如改为剪切板）
            ////复制：复制连接（偏移20单位）
            ////匹配属性：在两个连接间复制属性
            ////重置：将详细连接恢复为通用类型
            //try
            //{
            //    // 创建并显示WPF窗口
            //    var window = new GenericStructuralConnectionView(uiDoc);
            //    //// 设置Revit作为父窗口，确保窗口模态正确
            //    //var revitWindow = new WindowInteropHelper(uiDoc.Application.MainWindowHandle).Handle;
            //    //new WindowInteropHelper(window).Owner = revitWindow;
            //    var result = window.ShowDialog();
            //    return result == true ? Result.Succeeded : Result.Cancelled;
            //}
            //catch (Exception ex)
            //{
            //    message = ex.Message;
            //    return Result.Failed;
            //}

            ////GenerateFloor 
            ////Revit 基于选中墙体自动生成楼板工具，用户选中一组构成封闭轮廓的墙体，程序自动分析墙体轮廓并生成对应的结构楼板。
            //try
            //{
            //    var window = new GenerateFloorView(commandData.Application);
            //    window.ShowDialog();
            //    return Result.Succeeded;
            //}
            //catch (System.Exception ex)
            //{
            //    message = ex.Message;
            //    return Result.Failed;
            //}

            //////0501 官方代码测试
            ////界面有点问题，难得调用Document来源于creation的而不是db
            ////FrameBuilder 梁相关工具 主要是建模，也有改类型名和赋值类型的界面，暂未细分
            ////Revit 结构框架自动生成工具，用于批量创建柱、梁、支撑组成的建筑结构框架。核心功能
            ////批量生成结构柱：按矩阵排列创建结构柱
            ////自动生成梁：在柱顶之间创建水平梁
            ////自动生成支撑：在柱间创建 X 形斜撑
            ////多楼层支持：可生成多楼层结构
            ////类型管理：支持复制、编辑结构构件类型参数
            //try
            //{
            //    var window = new StructuralFrameBuilderView(commandData.Application);
            //    window.ShowDialog();
            //    return Result.Succeeded;
            //}
            //catch (System.Exception ex)
            //{
            //    message = ex.Message;
            //    return Result.Failed;
            //}

            //FreeFormElement Revit 负型块（Negative Block）创建工具，用于基于现有几何体创建与其互补的负空间族构件。
            //用户选择一个目标元素（如家具、设备）和一组边界曲线，自动创建一个与其外部形状互补的负型块家族实例。
            //TargetElementSelectionFilter	过滤目标元素	元素必须包含有效实体（Solid）
            //BoundarySelectionFilter 过滤边界曲线  必须是曲线元素，位于 XY 平面，支持 Line/ Arc
            //空间预留和模具设计工具，通过实体布尔运算自动生成几何体的互补形状，适用于需要精确切割或预留空间的设计场景。

            ////FoundationSlab 初始化有空值bug，GeometryDrawingService方法用于在界面重绘概要，是不是比GDI32更新？
            ////Revit 基础筏板自动创建工具，用于分析建筑模型中的底层楼板，并自动生成相应的基础筏板。核心功能
            ////识别底层楼板：找到最低标高处所有非结构楼板
            ////图形化预览：显示楼板轮廓和选中的八角形区域
            ////交互选择：用户可选择需要生成基础筏板的楼板
            ////批量生成：自动将选中的楼板替换为基础筏板
            //try
            //{
            //    var window = new FoundationSlabView(commandData.Application);
            //    window.ShowDialog();
            //    return Result.Succeeded;
            //}
            //catch (Exception ex)
            //{
            //    message = ex.Message;
            //    return Result.Failed;
            //}

            ////FireRating VB转C#  缺door的Parameter扩展方法，和Excel库 需求不大
            //Revit 防火等级参数管理工具集，包含三个命令：
            //1.ApplyParameter - 应用参数
            //创建共享参数文件（FireRating.txt）
            //在 Revit 中创建 / 获取 "Fire Rating" 共享参数
            //将参数绑定到"门"类别
            //2.ExportFireRating - 导出数据
            //获取所有门的 Fire Rating 参数值
            //导出到 Excel 文件（FireRating.xls）
            //3.ImportFireRating - 导入数据
            //从 Excel 文件读取防火等级数据
            //批量更新门的 Fire Rating 参数值
            //try
            //{
            //    var mainWindow = new FireRatingManagerView();
            //    var viewModel = new FireRatingManagerViewModel(commandData.Application);
            //    mainWindow.DataContext = viewModel;
            //    mainWindow.ShowDialog();
            //    return Result.Succeeded;
            //}
            //catch (Exception ex)
            //{
            //    message = ex.Message;
            //    return Result.Failed;
            //}

            ////SortFamilyParametersView  
            ////Revit 族文件参数排序工具，包含两个功能：
            ////1.SortFamilyFilesParamsForm
            ////功能：批量处理指定目录下的族文件（.rfa），对其中的参数进行排序
            ////操作：选择文件夹 → 选择排序方式（A→Z 或 Z→A）→ 批量修改族参数顺序
            ////2.SortLoadedFamiliesParamsForm
            ////功能：对当前 Revit 文档中已加载的族的参数进行排序
            ////操作：选择排序方式（A→Z 或 Z→A）→ 立即执行
            //try
            //{
            //    var mainWindow = new SortFamilyParametersView(commandData.Application);
            //    mainWindow.ShowDialog();
            //    return Result.Succeeded;
            //}
            //catch (Exception ex)
            //{
            //    message = ex.Message;
            //    return Result.Failed;
            //}

            //FabricationPartLayout  Revit 预制加工（Fabrication）工具集 暂无需求
            //Ancillaries	查询预制构件的附件信息	FabricationPart.GetPartAncillaryUsage()
            //ButtonGroupExclusions 设置服务按钮和组的排除项    OverrideServiceButtonExclusion(), SetServiceGroupExclusions()
            //ConvertToFabrication 将设计元素转换为预制构件    DesignToFabricationConverter.Convert()
            //CustomData 预制构件自定义数据管理工具，提供了查询和设置预制构件自定义数据的功能。
            //ExportToPCF Revit 预制构件 PCF 文件导出工具，用于将选中的预制构件导出为 PCF（Pipe Component File，管道组件文件）格式。
            //FabricationPartLayout Revit 预制构件布局自动创建工具，通过代码自动化生成复杂的 HVAC（暖通空调）和管道系统的预制构件布局。展示了 Revit Fabrication API 的几乎所有核心功能，适合作为预制加工二次开发的参考模板。
            //HangorRods Revit 预制构件吊架（Hanger）吊杆管理工具集，包含5个独立命令，用于控制吊架吊杆的挂接状态、长度和结构延伸长度。
            //OptimizeStraights Revit 预制构件直管长度优化工具，用于自动优化选中预制构件中直管段（Straight Parts）的长度。
            //PartInfo Revit 预制构件信息查询工具，用于显示选中预制构件的详细属性和参数。
            //PartRenumber Revit 预制构件自动重编号工具，用于为选中的预制构件自动生成并分配统一的编号（Item Number）。
            //SplitStraight Revit 预制直管段分割工具，用于将选中的预制直管构件从中点位置分割成两个独立的构件
            //StretchAndFit Revit 预制构件拉伸适配工具，用于将一个预制构件拉伸并连接到另一个目标构件上，自动生成中间的连接构件。。将起点构件（非直管 / 非三通 / 非吊架）的空闲连接器拉伸，使其连接到目标构件的空闲连接器上，并自动生成适配的连接段。

            //ExternalResourceUIServer 暂无需求
            //Revit 外部资源 UI 服务器（External Resource UI Server）示例，作为上一个问题的配套 UI 层组件，负责处理外部资源加载过程中的用户交互和结果反馈。
            //ExternalResourceDBServerRevit 外部资源服务器（External Resource Server）示例程序，演示了如何为 Revit 创建自定义的资源提供程序，用于动态提供图集数据（Keynotes）和 Revit 链接文件。核心功能
            //1.图集数据（Keynotes）服务
            //为德国和法国用户从虚拟数据库提供图集数据
            //为其他语言用户从本地文件提供图集数据
            //支持不同版本的图集数据管理
            //2.Revit 链接服务
            //从服务器提供 Revit 链接文件
            //自动缓存到本地
            //支持共享坐标更新回传
            //两者配合实现完整的 Revit 外部资源服务：DB Server 负责数据和逻辑，UI Server 负责交互和反馈。

            //ExternalCommandRegistration Revit 外部命令注册示例程序，演示了 Revit 插件开发的几个核心概念：外部命令、命令可用性控制和应用生命周期管理。

            //ExtensibleStorage 这两个相关方法DS转义非常糟糕 Gemini得全部重做
            //Revit 可扩展存储管理工具集，提供了查询和删除文档中所有扩展存储数据的功能。核心功能
            //存储查询（QueryStorage）：列出当前文档中所有包含扩展存储的 Schema 和元素信息
            //存储删除（DeleteStorage）：删除当前文档中所有扩展存储数据
            //工具类（StorageUtility）：提供通用的存储查询辅助方法
            //new ExtensibleStorageStatistics(commandData);
            //new ExtensibleStorageDeletion(commandData);

            //ExtensibleStorageManager
            ////Revit 可扩展存储（Extensible Storage）管理工具，用于创建、管理和操作 Revit 的扩展存储数据。核心功能
            ////创建 Schema：在 Revit 文档中创建自定义数据架构
            ////存储数据：将数据存储到 ProjectInformation 元素中
            ////导入 / 导出 XML：支持 Schema 的序列化和反序列化
            ////查询编辑：查询和编辑已存储的 Entity 数据
            ////支持复杂数据类型：基本类型、数组、字典、子实体等
            //try
            //{
            //    var document = commandData.Application.ActiveUIDocument.Document;
            //    var addInId = commandData.Application.ActiveAddInId.GetGUID().ToString();
            //    var mainWindow = new ExtensibleStorageManagerView();
            //    var viewModel = new ExtensibleStorageManagerViewModel(document, addInId);
            //    mainWindow.DataContext = viewModel;
            //    mainWindow.ShowDialog();
            //    return Result.Succeeded;
            //}
            //catch (Exception ex)
            //{
            //    message = ex.Message;
            //    return Result.Failed;
            //}


            //ErrorHandling Gemini分析过，待想办法批量处理

            //EnergyAnalysisModel 跳过，没啥需求

            //ElementsBatchCreator 增补DocumentExtensions中方法待测试
            ////Revit 批量创建元素工具，演示如何使用 Revit API 的批量创建方法一次性生成多种类型的建筑元素。
            ////核心功能
            ////批量创建区域（Area）：在指定楼层创建多个区域
            ////批量创建结构柱（Column）：在指定位置创建多个结构柱
            ////批量创建房间（Room）：在封闭区域内创建多个房间
            ////批量创建文字注释（TextNote）：在视图中创建多个文字注释
            ////批量创建墙体（Wall）：创建多种形状的墙体（直墙、弧形墙）
            //try
            //{
            //    var batchCreator = new ElementsBatchCreator(commandData);
            //    var result = batchCreator.CreateAllElements();

            //    if (!result)
            //    {
            //        message = "部分元素创建失败，请查看详细信息。";
            //        return Result.Failed;
            //    }

            //    return Result.Succeeded;
            //}
            //catch (Exception ex)
            //{
            //    message = $"批量创建失败：{ex.Message}";
            //    return Result.Failed;
            //}




            ////DynamicModelUpdate =>  DMUAssociativeSectionUpdate
            ////计算旋转角度 应提取作为公共方法
            ////Revit 关联剖面更新器，实现剖面视图与窗户元素的动态关联，当窗户移动或修改时，剖面视图自动跟随调整位置和方向。核心功能
            ////关联剖面与窗户：用户选择一个剖面视图和一个窗户，建立二者之间的关联关系
            ////自动跟随更新：当窗户位置或方向改变时，剖面视图自动调整位置和角度
            ////智能方向计算：根据窗户的朝向自动计算剖面的旋转角度
            ////动态注册 / 注销：使用 Revit Updater 机制监听元素变化
            //new DMUAssociativeSectionUpdate(commandData);

            //DWGFamilyCreation 项目如果需要表达是否有必要重建
            // 族文档 DWG 导入工具，主要功能是在族编辑环境中将 DWG 文件导入并创建参数化族。
            //核心功能
            //验证文档类型：确保当前文档是族文档（.rfa）
            //查找目标视图：在族文档中找到名为 "Ref. Level" 的非模板楼层平面视图
            //导入 DWG 文件：将指定的 Desk.dwg 文件导入到族文档中
            //添加类型参数：自动添加两个族类型参数记录导入信息

            ////Revit 视图复制工具，主要功能是将一个文档中的明细表视图和草图视图复制到另一个打开的文档中。核心功能
            ////复制明细表（ViewSchedule）：将源文档中的明细表复制到目标文档
            ////复制草图视图（ViewDrafting）：复制草图视图及其内部的所有详图元素
            ////智能处理依赖关系：使用 CopyElements API 确保依赖元素只被复制一次
            ////自动处理冲突：通过自定义处理器处理重复类型名称和警告消息
            ////技术要点
            ////要求同时打开两个 Revit 文档
            ////使用 ElementTransformUtils.CopyElements 跨文档复制
            ////使用 IDuplicateTypeNamesHandler 自动处理类型名称冲突
            ////使用 IFailuresPreprocessor 过滤警告消息
            //DuplicateView duplicateView = new DuplicateView();
            //var app = commandData.Application.Application;
            //var currentDoc = commandData.Application.ActiveUIDocument.Document;
            //// 查找目标文档：必须恰好有两个打开的文档
            //var openDocs = app.Documents.Cast<Document>().ToList();
            //if (openDocs.Count != 2)
            //{
            //    TaskDialog.Show("无目标文档",
            //        "此工具需要同时打开两个文档（一个源文档和一个目标文档）");
            //    return Result.Cancelled;
            //}
            //// 确定目标文档（不是当前文档的那个）
            //var targetDoc = openDocs.FirstOrDefault(d => d.Title != currentDoc.Title);
            //if (targetDoc == null)
            //{
            //    message = "无法确定目标文档";
            //    return Result.Failed;
            //}
            //// 收集当前文档中的所有明细表和草图视图
            //var collector = new FilteredElementCollector(currentDoc);
            //// 筛选明细表和草图视图类型
            //var viewTypes = new List<Type> { typeof(ViewSchedule), typeof(ViewDrafting) };
            //var multiFilter = new ElementMulticlassFilter(viewTypes);
            //collector.WherePasses(multiFilter);
            //// 跳过视图特定明细表（如修订明细表），这些不能独立复制
            //collector.WhereElementIsViewIndependent();
            //// 复制明细表
            //var schedules = collector.OfType<ViewSchedule>().ToList();
            //if (schedules.Any())
            //{
            //    DuplicateViewUtils.DuplicateSchedules(currentDoc, schedules, targetDoc);
            //}
            //// 复制草图视图及其内容
            //var draftingViews = collector.OfType<ViewDrafting>().ToList();
            //var newDetailCount = 0;
            //if (draftingViews.Any())
            //{
            //    newDetailCount = DuplicateViewUtils.DuplicateDraftingViews(
            //        currentDoc, draftingViews, targetDoc);
            //}
            //// 显示统计结果
            //TaskDialog.Show("复制统计", $"复制完成：\n" + $"\t{schedules.Count} 个明细表。\n" +
            //    $"\t{draftingViews.Count} 个草图视图。\n" + $"\t{newDetailCount} 个新详图元素。");


            //高级功能先有个印象以后再考虑是否深挖
            //DuplicateGraphics管理通过 DirectContext3D 绘制的自定义图形。主要包含两个外部命令：
            //1.CommandDuplicateGraphics - 复制图形命令
            //2.CommandClearExternalGraphics - 清除图形命令
            //DirectContext3D 是 Revit API 提供的底层图形绘制接口，允许开发者：
            //直接绘制：绕过 Revit 元素系统，直接在视图上绘制几何图形
            //高性能渲染：适合大量动态图形的实时显示
            //临时视觉反馈：用于选择高亮、分析结果展示、测量标注等

            //DoorSwing 门摆向修改，如果跟族关联，那似乎没啥需求
            //Revit插件，用于管理门的开向（左开/右开）和相关参数。我来分析程序功能并使用C# 7.3语法和WPF MVVM模式重构。
            //初始化门开向：根据门的几何形状和国家标准，设置门的开向参数（左开 / 右开 / 双开等）
            //更新门参数：更新门实例的开向、内外门标志、从房间 / 到房间信息
            //更新门几何：根据从房间 / 到房间信息调整门的几何方向
            //自动保存更新：在文档保存时自动更新门信息

            ////0430 官方代码测试
            //DockableDialogs 可停靠窗口实现，等需要时再测暂跳过

            //DisplacementElementAnimation
            //Revit 位移结构模型动画工具，主要功能包括：
            //位移元素动画：对 Revit 中设置了位移（Displacement）的结构模型元素进行动画演示
            //两种动画模式：
            //自动播放模式：连续播放完整动画
            //步进模式：手动控制动画步骤，逐帧推进

            //CommandDisabler 不好测试，没啥场合应用
            //Revit命令禁用工具，主要功能包括：
            //启动时拦截指定命令：在Revit启动时查找并绑定目标命令
            //禁用命令执行：当用户尝试执行该命令时，拦截并显示提示信息
            //关闭时清理绑定：在Revit关闭时移除命令绑定

            ////FindSouthFacingWalls 找南向墙、南向窗的方法比较乱 参考价值一般
            ////Revit朝南外墙查找工具，主要功能包括：
            ////收集外墙：筛选出所有外墙类型（Function参数为Exterior）
            ////计算朝向：通过墙的方向向量计算外法线方向
            ////判断朝南：检查法线方向是否在朝南范围内（±45度）
            ////两种模式：支持使用项目北向或默认坐标系
            ////选中结果：将朝南的外墙添加到当前选择集中
            //FindSouthFacingWalls findSouthFacingWalls = new FindSouthFacingWalls(commandData);

            ////DimensionHorizontalMover 待测试
            ////Revit尺寸标注水平移动工具，主要功能包括：
            ////沿尺寸线方向移动引线：将尺寸标注的引线端点沿尺寸线方向水平移动固定距离（-10单位）
            ////支持多线段尺寸：处理包含多个线段的尺寸标注
            ////批量处理：支持同时处理多个选中的尺寸标注
            //DimensionHorizontalMover dimensionHorizontalMover = new DimensionHorizontalMover(commandData);

            ////DimensionMoveToPickedPoint 待测试
            ////Revit尺寸标注引线端点移动工具，主要功能包括：
            ////选择尺寸标注：获取当前选中的尺寸标注元素
            ////拾取目标点：让用户在视图中拾取一个点作为引线的新端点
            ////移动引线端点：将尺寸标注的引线端点移动到拾取的位置
            ////处理多线段尺寸：支持包含多个线段的尺寸标注，按顺序偏移设置各线段端点
            //DimensionMoveToPickedPoint moveToPickedPoint = new DimensionMoveToPickedPoint(commandData);

            //DesignOptionView 没啥实际作用
            ////Revit设计选项查看工具，主要功能包括：
            ////收集设计选项：从当前Revit文档中收集所有设计选项
            ////显示列表：在对话框中以列表形式显示所有设计选项的名称
            ////查看信息：用户可以查看当前文档中存在哪些设计选项
            //try
            //{
            //    var window = new DesignOptionView();
            //    var viewModel = new DesignOptionViewModel(commandData);
            //    window.DataContext = viewModel;

            //    // 获取Revit主窗口并设置为所有者
            //    var revitHandle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            //    if (revitHandle != IntPtr.Zero)
            //    {
            //        var helper = new WindowInteropHelper(window);
            //        helper.Owner = revitHandle;
            //    }

            //    return window.ShowDialog() == true ? Result.Succeeded : Result.Cancelled;
            //}
            //catch (Exception ex)
            //{
            //    message = ex.Message;
            //    return Result.Failed;
            //}

            //DimensionCleaner 功能可用，需要先选择有点操作反直觉 增补DocumentExtensions中方法待测试
            //Revit尺寸标注批量删除工具，主要功能包括：
            //获取Revit选中的元素：从当前选择集中获取所有选中的元素
            //筛选未固定的尺寸标注：过滤出类型为Dimension且未被固定(Pinned = false)的尺寸标注
            //批量删除：在事务中批量删除所有符合条件的尺寸标注
            //DimensionCleaner dimensionCleaner = new DimensionCleaner(commandData);

            ////DeckPropertyView 对理解楼板有点用，实际作用不大
            ////Revit楼板 / 压型钢板属性查看工具，主要功能包括：
            ////选择楼板 / 压型钢板：从Revit中选中一个或多个楼板元素
            ////解析复合结构：读取楼板类型中的复合结构层
            ////识别压型钢板层：检测并专门处理压型钢板（Deck）层
            ////显示属性信息：展示材料、厚度、压型钢板轮廓参数等详细信息
            //try
            //{
            //    var window = new DeckPropertyView();
            //    var viewModel = new DeckPropertyViewModel(commandData);
            //    window.DataContext = viewModel;
            //    // 获取Revit主窗口并设置为所有者
            //    var revitHandle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            //    if (revitHandle != IntPtr.Zero)
            //    {
            //        var helper = new WindowInteropHelper(window);
            //        helper.Owner = revitHandle;
            //    }
            //    return window.ShowDialog() == true ? Result.Succeeded : Result.Cancelled;
            //}
            //catch (Exception ex)
            //{
            //    message = ex.Message;
            //    return Result.Failed;
            //}

            ////DatumPropagationView 3个工具的界面增加了不少元素，跟原始意图相差很大
            ////Revit基准线（轴线 / 标高 / 参照平面）范围传播工具，主要功能包括：
            ////选择基准线：从当前选中的基准线中获取一个作为源
            ////获取传播视图列表：获取该基准线可以传播到的所有视图
            ////选择目标视图：用户勾选需要应用相同范围设置的视图
            ////传播范围：将当前视图中的基准线范围设置传播到选中的目标视图
            //try
            //{
            //    var window = new DatumPropagationView(commandData);
            //    var viewModel = new DatumPropagationViewModel(commandData);
            //    window.DataContext = viewModel;
            //    // 获取Revit主窗口并设置为所有者
            //    var revitHandle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            //    if (revitHandle != IntPtr.Zero)
            //    {
            //        var helper = new WindowInteropHelper(window);
            //        helper.Owner = revitHandle;
            //    }
            //    return window.ShowDialog() == true ? Result.Succeeded : Result.Cancelled;
            //}
            //catch (Exception ex)
            //{
            //    message = ex.Message;
            //    return Result.Failed;
            //}

            ////DatumAlignment.可视化修改？没啥意义吧
            ////Revit基准线（轴线/标高/参照平面）对齐工具，主要功能包括：
            ////选择参考基准线：从选中的基准线中选择一条作为对齐参考
            ////对齐其他基准线：将所有选中的基准线按照参考基准线的方向对齐（X / Y / Z方向）
            //try
            //{
            //    var window = new DatumAlignmentView(commandData);
            //    var viewModel = new DatumAlignmentViewModel(commandData);
            //    window.DataContext = viewModel;
            //    // 获取Revit主窗口并设置为所有者
            //    var revitHandle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            //    if (revitHandle != IntPtr.Zero)
            //    {
            //        var helper = new WindowInteropHelper(window);
            //        helper.Owner = revitHandle;
            //    }
            //    return window.ShowDialog() == true ? Result.Succeeded : Result.Cancelled;
            //}
            //catch (Exception ex)
            //{
            //    message = ex.Message;
            //    return Result.Failed;
            //}

            ////DatumStyleModification 可视化修改？没啥意义吧
            ////Revit基准线（标高 / 轴线 / 参照平面）样式修改工具，主要功能包括：
            ////控制基准线两端的气泡显示 / 隐藏
            ////添加 / 删除基准线两端的弯头（肘部）
            ////切换基准线端点的2D / 3D范围模式
            //try
            //{
            //    var window = new DatumStyleModificationView(commandData);
            //    // 获取Revit主窗口句柄并设置为所有者
            //    var revitWindow = System.Diagnostics.Process
            //        .GetCurrentProcess()
            //        .MainWindowHandle;
            //    if (revitWindow != IntPtr.Zero)
            //    {
            //        var helper = new System.Windows.Interop.WindowInteropHelper(window);
            //        helper.Owner = revitWindow;
            //    }
            //    return window.ShowDialog() == true ? Result.Succeeded : Result.Cancelled;
            //}
            //catch (Exception ex)
            //{
            //    message = ex.Message;
            //    return Result.Failed;
            //}

            //CreateCurvedBeam 问题是
            ////Revit弧形梁创建工具，主要功能包括：
            ////选择梁类型：从项目中加载所有结构框架族类型
            ////选择标高：选择梁的放置标高
            ////创建三种曲线梁：圆弧梁、椭圆弧梁、样条曲线梁
            //// 创建WPF窗口并设置为Revit主窗口的所有者
            //var mainWindow = new CreateCurvedBeamView(commandData);
            //mainWindow.Owner = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle != IntPtr.Zero
            //    ? System.Windows.Interop.HwndSource.FromHwnd(
            //        System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle)?.RootVisual as CreateCurvedBeamView
            //    : null;
            ////// 设置数据上下文，传入commandData
            ////var viewModel = new CreateCurvedBeamViewModel(commandData);
            ////mainWindow.DataContext = viewModel;
            //// 显示模态窗口
            ////mainWindow.ShowDialog() == true ? Result.Succeeded : Result.Cancelled;
            //mainWindow.ShowDialog();

            ////CurtainWallGrid 也包括了MathTools的Vector部分还有Matrix 超长方法代码超过5k行，仅生成主干框架部分后续待补
            ////Revit幕墙网格编辑工具，主要功能包括：
            ////创建幕墙：在指定视图中绘制基线，选择幕墙类型创建幕墙
            ////编辑网格：添加 / 删除U / V方向网格线、添加 / 恢复网格线段、锁定 / 解锁网格线、移动网格线
            ////添加 / 删除竖梃
            ////查看网格属性
            //var window = new CreateCurtainWallView(commandData);
            //var result = window.ShowDialog();

            //CurtainSystem 体系比较庞大，先绕过Command 人口方法，Vector方法提出来先看看四维齐次坐标向量类
            //四维向量类，主要用于三维空间计算（第四分量W默认为1.0，常用于齐次坐标）。主要功能包括：
            //存储X、Y、Z、W四个分量
            //支持从Revit的XYZ类型转换
            //计算两个向量的叉积（得到法向量）
            //原始代码存在一个小问题：CrossProduct方法没有设置结果向量的W分量，会使用默认值1.0。在齐次坐标系中，叉积结果应是方向向量（垂直于平面的法向量），W应为0而非1。改写版本已修正。

            //CreateWallsUnderBeam 大致成功，待完善
            ////Revit 在梁下方创建墙体插件，主要功能：
            ////选择梁 - 用户选择一根或多根水平梁
            ////验证梁为水平 - 检查每根梁的分析模型线是否为水平
            ////选择墙体类型 - 通过对话框选择墙体类型和是否结构墙
            ////创建墙体 - 沿每根梁的分析模型线创建墙体，位于梁下方
            //try
            //{
            //    var window = new CreateWallsUnderBeamView(commandData);
            //    var result = window.ShowDialog();
            //    return result == true ? Result.Succeeded : Result.Cancelled;
            //}
            //catch (System.Exception ex)
            //{
            //    message = ex.Message;
            //    return Result.Failed;
            //}

            ////0429 官方代码测试
            ////WallFromBeamsCreatView，有GeometryHelper参考，DataLoadingService类重叠
            ////Revit 基于梁轮廓创建墙体插件，主要功能：
            ////选择梁 - 用户选择多根梁（首尾相连形成闭合轮廓）
            ////验证垂直平面 - 检查所有梁是否在同一垂直平面内
            ////验证闭合轮廓 - 检查梁是否能形成闭合轮廓
            ////选择墙体类型 - 通过对话框选择墙体类型和是否结构墙
            ////创建墙体 - 沿着梁轮廓创建墙体，并设置标高和偏移
            //try
            //{
            //    var window = new WallFromBeamsCreatView(commandData);
            //    var result = window.ShowDialog();
            //    return result == true ? Result.Succeeded : Result.Cancelled;
            //}
            //catch (System.Exception ex)
            //{
            //    message = ex.Message;
            //    return Result.Failed;
            //}

            ////SectionViewCreator 有XYZMath方法可调用 功能基本可直接使用
            ////Revit 剖面视图自动创建插件，主要功能：
            ////选择线性元素 - 支持墙体(Wall)、梁(Beam)、楼板(Floor)
            ////计算截面位置 - 自动计算元素的中点作为剖面位置
            ////计算视图方向 - 根据元素类型确定剖面的方向和范围
            ////创建剖面视图 - 在元素中点处生成垂直剖面图
            ////创建详图视图 - 额外功能，创建空白详图视图
            ////try
            ////{
            ////    var document = commandData.Application.ActiveUIDocument.Document;
            ////    var transaction = new Transaction(document, "创建详图视图");
            ////    transaction.Start();
            ////    var collector = new FilteredElementCollector(document);
            ////    var viewFamilyType = collector
            ////        .OfClass(typeof(ViewFamilyType))
            ////        .Cast<ViewFamilyType>()
            ////        .FirstOrDefault(v => v.ViewFamily == ViewFamily.Drafting);
            ////    if (viewFamilyType is null)
            ////    {
            ////        return Result.Failed;
            ////    }
            ////    else
            ////    {  
            ////        ViewDrafting draftingView = ViewDrafting.Create(document, viewFamilyType.Id);
            ////        if (draftingView is null)
            ////        {
            ////            message = "无法创建详图视图";
            ////            transaction.RollBack();
            ////            return Result.Failed;
            ////        }
            ////        transaction.Commit();
            ////        TaskDialog.Show("Revit", $"详图视图创建成功！视图名称: {draftingView.Name}");
            ////    }
            ////    return Result.Succeeded;
            ////}
            ////catch (Exception ex)
            ////{
            ////    message = ex.Message;
            ////    return Result.Failed;
            ////}
            ////下一句与上面是两种方式生成剖面
            //SectionViewCreator sectionViewCreator = new SectionViewCreator(commandData);

            ////TrussCreator GeometryExtensions与AirHandlerCreator 共享partial class 
            ////Revit 桁架族创建插件，主要功能：
            ////在桁架族文档中创建单榀桁架 - 使用参考平面构建桁架几何
            ////创建桁架构件 - 生成上弦杆、下弦杆、腹杆等
            ////添加对齐约束 - 将桁架线锁定到参考平面
            ////添加角度尺寸约束 - 确保腹杆角度可调且稳定
            //TrussCreator trussCreator =new TrussCreator(commandData);

            ////主要功能用不上，有一些几何基本方法GeometryDataService GeometryService(RevitBeamSystemCreatorView也有)可参考
            ////Revit 区域钢筋创建插件，主要功能：
            ////选择结构元素 - 支持墙体(Wall)和楼板(Floor)
            ////验证几何条件 - 检查选中的元素是否为矩形、垂直 / 水平面
            ////设置钢筋参数 - 通过属性网格配置布局规则、钢筋层方向等
            ////创建区域钢筋 - 根据配置参数自动生成AreaReinforcement
            //try
            //{
            //    var window = new AreaReinforcementCreatView(commandData);
            //    var result = window.ShowDialog();
            //    return result == true ? Result.Succeeded : Result.Cancelled;
            //}
            //catch (System.Exception ex)
            //{
            //    message = ex.Message;
            //    return Result.Failed;
            //}

            //////CreateSharedParameter 共享参数方法 没成功 ，待深化测试
            //////创建并绑定共享参数,不存在则创建,绑定到墙体类别,不设置值，目标类别级别的绑定
            //CreateSharedParameter createSharedParameter = new CreateSharedParameter(commandData);
            //////SetSharedParameterCommand
            //////Revit 共享参数批量设置插件（VB.NET原始代码），主要功能：
            //////读取共享参数文件 - 从共享参数文件中获取名为"APIParameter"的参数定义
            //////筛选元素 - 获取所有非类型元素（实例元素）
            //////过滤墙体 - 只处理类别为"Walls"的元素
            //////批量设置参数 - 为所有墙元素设置共享参数值为"Hello Revit"
            //SetSharedParameterCommand setSharedParameterCommand = new SetSharedParameterCommand(commandData);

            ////PatternManagerView 缺导入窗体，方法执行细节有错误待细化
            //////Revit 图案样式应用插件，主要功能：
            //////填充图案管理 - 显示并应用填充图案(FillPattern)到表面或切割面
            //////线型图案管理 - 显示并应用线型图案(LinePattern)到网格线
            //////图案创建 - 支持创建简单填充图案、复杂填充图案和线型图案
            //try
            //{
            //    var window = new PatternManagerView(commandData);
            //    var result = window.ShowDialog();
            //    return result == true ? Result.Succeeded : Result.Cancelled;
            //}
            //catch (System.Exception ex)
            //{
            //    message = ex.Message;
            //    return Result.Failed;
            //}

            //WallDimensionDemo给选择墙体加标注，逻辑基本无问题但没成功
            //Revit 墙体尺寸标注插件，主要功能：
            //选择墙体 - 用户选择一面或多面基础墙(Basic Wall)
            //分析分析模型 - 获取墙体的分析模型中的非垂直线段
            //创建尺寸标注 - 从墙体起点到终点创建尺寸标注
            //只支持2D视图 - 在3D视图或图纸视图中会提示错误
            //WallDimensionDemo wallDimensionDemo = new WallDimensionDemo(commandData);

            ////CreateBeamSystem有界面，基本完整，似乎不实用，有的底层GeometryUtil	几何工具（线条排序、共面检查）可参考，预览窗口实现逻辑
            ////Revit 梁系统创建插件，主要功能：
            ////从选中的梁构建闭合轮廓 - 用户选择首尾相连的梁，自动排序形成闭合多边形轮廓
            ////可视化预览 - 在窗口中显示轮廓的2D示意图，支持方向切换
            ////设置梁系统参数 - 通过属性网格配置布局规则、梁类型等
            ////创建梁系统 - 根据设置的参数自动生成Revit梁系统
            //try
            //{
            //    var transaction = new Transaction(commandData.Application.ActiveUIDocument.Document, "创建梁系统");
            //    var window = new RevitBeamSystemCreatorView(commandData);
            //    var result = window.ShowDialog();
            //    return result == true ? Result.Succeeded : Result.Cancelled;
            //}
            //catch (System.Exception ex)
            //{
            //    message = ex.Message;
            //    return Result.Failed;
            //}            

            //AirHandlerCreator无界面，待重构看是否有需求。有几何和实体族建立逻辑流程可参考
            //Revit 空气处理单元(AHU)族创建插件，主要功能：
            //创建自定义机械族 - 在族编辑器中创建空气处理单元
            //构建复杂几何体 - 通过5个拉伸体组合形成设备外形（3个矩形拉伸 + 2个圆形拉伸）
            //添加连接件 - 创建4个系统连接器：
            //送风管连接器(Supply Air)
            //回风管连接器(Return Air)
            //供水管连接器(Supply Hydronic)
            //回水管连接器(Return Hydronic)
            //参数设置 - 设置连接器尺寸、流量、流向等参数
            //合并几何体 - 将所有拉伸体合并为单个实体
            //AirHandlerCreator airHandlerCreator = new AirHandlerCreator(commandData); 

            ////CompoundStructure无界面，待重构看是否有需求
            ////Revit 墙体复合结构创建插件，主要功能：
            ////为选中的墙体应用复合结构 - 创建多层墙体构造（饰面层、基层、结构层、膜层等）
            ////创建自定义材质 - 砖和混凝土材质，包含结构和热工属性
            ////分割墙体区域 - 在墙体中创建新的区域分区
            ////添加墙饰条和墙嵌条 - 在指定位置添加扫掠和凹槽
            ////设置复合结构参数 - 结构层索引、包络层、包裹参与等
            //WallCompoundStructureCommand wallCompound = new WallCompoundStructureCommand(commandData);

            //ChangesMonitor变更追踪器 - Revit元素变更监控 没啥使用可能，跳过

            //FormatStatusExtensions 待测试
            //Revit 文本注释格式批量转换插件，主要功能：
            //查找所有TextNote元素 - 遍历当前文档中的所有文本注释
            //检测大写格式状态 - 使用GetAllCapsStatus()判断是否已全大写
            //批量应用全大写格式 - 对未全大写的文本注释应用AllCaps格式
            //支持部分格式检测 - 可检测单个字符或字符范围的格式状态

            //////0428 官方代码测试
            //CancelSave逻辑已基本实现，但内部有个LogManager可以看一下思路与自己完成的是否一致。ds改写后变动很大。

            //Revit 结构边界条件编辑器插件，没啥使用可能，跳过
            //选择结构元素（柱、梁、墙、楼板等）
            //查看 / 编辑边界条件属性（固定、铰支、滚动支座、自定义）
            //支持三种边界条件类型：点、线、面
            //设置弹簧刚度（用户自定义状态）

            //BRepBuilder没啥使用可能，跳过

            //ParameterBindingBrowserWindow基本可用，难得
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

            //UniqueIdManagerWindow插件是否必要？
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

            //PipeCollisionResolver默认为自动执行，需要调整
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

            //AutoTagRoomsWindow可以借鉴RoomEntity的思路，加标记相比系统级别自动没想出太多优点。
            ////Revit插件，用于自动为房间添加标记：
            ////选择楼层 - 选择需要添加房间标记的楼层
            ////选择标记类型 - 选择使用的房间标记类型
            ////显示房间列表 - 展示该楼层所有房间及已有标记数量
            ////自动标记 - 为所有未标记房间自动添加标记
            //var viewModel = new AutoTagRoomsViewModel(commandData);
            //var window = new AutoTagRoomsWindow { DataContext = viewModel };
            //viewModel.CloseAction = window.Close;
            //window.ShowDialog();

            //PrintMonitorWindow 比较复杂，没啥实用性，只实现了界面
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

            //DuctSystemWindow有点意思的功能，可以考虑细化按区域布置，风管尺寸变径、分叉如何考虑，要改为非模态调用
            ////Revit插件，用于自动创建空调风管系统：
            ////连接设备 - 将1个送风设备和2个末端设备用风管连接
            ////智能路径规划 - 自动计算最优的风管布置路径
            ////创建管件 - 自动添加弯头、三通等管件
            ////系统日志 - 记录创建的风管系统详细信息
            //var viewModel = new DuctSystemViewModel(commandData);
            //var window = new DuctSystemWindow { DataContext = viewModel };
            //viewModel.CloseAction = window.Close;
            //window.ShowDialog();

            //AddParameterWindow 待测试
            //Revit插件，用于批量向族文件添加参数：
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

            //AutoJoinWindow界面和代码完整，但似乎没有实际执行，选择后未识别到符合条件的构件
            ////这是一个Revit插件，用于自动连接重叠的几何形体（如体量、通用模型）：
            ////检测重叠 - 通过几何相交测试判断两个实体是否重叠
            ////查找重叠组 - 递归查找所有相互重叠的构件集合
            ////合并几何 - 使用CombineElements将重叠的几何合并为一个整体
            ////两种模式 - 支持选中构件合并或全文档自动合并
            //var viewModel = new AutoJoinViewModel(commandData);
            //var window = new AutoJoinWindow { DataContext = viewModel };
            //viewModel.CloseAction = window.Close;
            //window.ShowDialog();

            //AreaReinforcementParameterWindow WPF xaml语法存在问题
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

            //AreaReinforcementWindow
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

            //ExportToExcelWindow 载入构件存在崩溃问题，未涉及到导出环境
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

            //AnalyticalSupportWindow
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
            //ZoneEditorMainWindow 待测试
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

            //AllViewsWindow 待测试
            ////读取视图放到图纸上
            //var viewModel = new MainViewModel(doc);
            //var window = new AllViewsWindow { DataContext = viewModel };
            //return window.ShowDialog() == true ? Result.Succeeded : Result.Cancelled;

            return Result.Succeeded;
        }

    }
}
