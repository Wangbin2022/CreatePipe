using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// PlaceFamilyInstanceView.xaml 的交互逻辑
    /// </summary>
    public partial class PlaceFamilyInstanceView : Window
    {
        public PlaceFamilyInstanceView(FamilyInstanceCreator creator, BasedType baseType)
        {
            InitializeComponent();
            var viewModel = new PlaceFamilyInstanceViewModel(creator, baseType);
            viewModel.CloseWindow = Close;

            DataContext = viewModel;
        }
    }
    /// <summary>
    /// 主窗体ViewModel - 管理面选择和族实例创建
    /// </summary>
    public class PlaceFamilyInstanceViewModel : ObserverableObject
    {
        private readonly FamilyInstanceCreator _creator;
        private readonly BasedType _baseType;
        private FaceItem _selectedFace;
        private FamilySymbolItem2 _selectedFamilySymbol;
        private PointData _firstPoint;
        private PointData _secondPoint;
        private string _windowTitle;
        private string _firstLabel;
        private string _secondLabel;
        private bool _isCreating;

        public ObservableCollection<FaceItem> Faces { get; } = new ObservableCollection<FaceItem>();
        public ObservableCollection<FamilySymbolItem2> FamilySymbols { get; } = new ObservableCollection<FamilySymbolItem2>();

        public FaceItem SelectedFace
        {
            get => _selectedFace;
            set
            {
                _selectedFace = value;
                OnPropertyChanged();
                if (value != null) UpdatePointsFromFace(value.Index);
            }
        }

        public FamilySymbolItem2 SelectedFamilySymbol
        {
            get => _selectedFamilySymbol;
            set { _selectedFamilySymbol = value; OnPropertyChanged(); }
        }

        public PointData FirstPoint
        {
            get => _firstPoint;
            set { _firstPoint = value; OnPropertyChanged(); }
        }

        public PointData SecondPoint
        {
            get => _secondPoint;
            set { _secondPoint = value; OnPropertyChanged(); }
        }

        public string WindowTitle
        {
            get => _windowTitle;
            set { _windowTitle = value; OnPropertyChanged(); }
        }

        public string FirstLabel
        {
            get => _firstLabel;
            set { _firstLabel = value; OnPropertyChanged(); }
        }

        public string SecondLabel
        {
            get => _secondLabel;
            set { _secondLabel = value; OnPropertyChanged(); }
        }

        public bool IsCreating
        {
            get => _isCreating;
            set { _isCreating = value; OnPropertyChanged(); }
        }

        public ICommand CreateCommand { get; }
        public ICommand CancelCommand { get; }

        public PlaceFamilyInstanceViewModel(FamilyInstanceCreator creator, BasedType baseType)
        {
            _creator = creator;
            _baseType = baseType;

            InitializeUI();
            LoadFaces();
            LoadFamilySymbols();

            CreateCommand = new BaseBindingCommand(_ => CreateInstance(), _ => !IsCreating);
            CancelCommand = new BaseBindingCommand(_ => CloseWindow?.Invoke());
        }

        private void InitializeUI()
        {
            switch (_baseType)
            {
                case BasedType.Point:
                    WindowTitle = "放置基于点的族实例";
                    FirstLabel = "位置点:";
                    SecondLabel = "方向向量:";
                    FirstPoint = PointData.FromXYZ(new XYZ(0, 0, 0));
                    SecondPoint = PointData.FromXYZ(new XYZ(1, 0, 0));
                    break;
                case BasedType.Line:
                    WindowTitle = "放置基于线的族实例";
                    FirstLabel = "起点:";
                    SecondLabel = "终点:";
                    break;
            }
        }

        private void LoadFaces()
        {
            Faces.Clear();
            for (int i = 0; i < _creator.FaceNameList.Count; i++)
            {
                Faces.Add(new FaceItem
                {
                    Name = _creator.FaceNameList[i],
                    Face = _creator.FaceList[i],
                    Index = i
                });
            }
            if (Faces.Any())
                SelectedFace = Faces[0];
        }

        private void LoadFamilySymbols()
        {
            FamilySymbols.Clear();
            for (int i = 0; i < _creator.FamilySymbolNameList.Count; i++)
            {
                FamilySymbols.Add(new FamilySymbolItem2
                {
                    DisplayName = _creator.FamilySymbolNameList[i],
                    Symbol = _creator.FamilySymbolList[i],
                    Index = i
                });
            }
            if (FamilySymbols.Any())
                SelectedFamilySymbol = FamilySymbols.FirstOrDefault(f => f.Index == _creator.DefaultFamilySymbolIndex)
                                    ?? FamilySymbols[0];
        }

        private void UpdatePointsFromFace(int faceIndex)
        {
            var bbox = _creator.GetFaceBoundingBox(faceIndex);
            var center = (bbox.Min + bbox.Max) / 2;

            switch (_baseType)
            {
                case BasedType.Point:
                    FirstPoint = PointData.FromXYZ(center);
                    SecondPoint = PointData.FromXYZ(new XYZ(1, 0, 0));
                    break;
                case BasedType.Line:
                    FirstPoint = PointData.FromXYZ(bbox.Min);
                    SecondPoint = PointData.FromXYZ(bbox.Max);
                    break;
            }
        }

        private void CreateInstance()
        {
            IsCreating = true;

            try
            {
                using (var transaction = new Transaction(_creator.RevitDoc.Document, "创建族实例"))
                {
                    transaction.Start();

                    bool success = _baseType == BasedType.Point
                        ? _creator.CreatePointFamilyInstance(
                            FirstPoint.ToXYZ(), SecondPoint.ToXYZ(),
                            SelectedFace?.Index ?? 0, SelectedFamilySymbol?.Index ?? 0)
                        : _creator.CreateLineFamilyInstance(
                            FirstPoint.ToXYZ(), SecondPoint.ToXYZ(),
                            SelectedFace?.Index ?? 0, SelectedFamilySymbol?.Index ?? 0);

                    transaction.Commit();

                    if (success)
                    {
                        CloseWindow?.Invoke();
                    }
                    else
                    {
                        TaskDialog.Show("Revit", "线段与该面垂直，请重新选择位置。");
                    }
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Revit", $"创建失败：{ex.Message}");
            }
            finally
            {
                IsCreating = false;
            }
        }

        public Action CloseWindow { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    /// <summary>
    /// 面信息项 - 用于下拉选择
    /// </summary>
    public class FaceItem
    {
        public string Name { get; set; }
        public Autodesk.Revit.DB.Face Face { get; set; }
        public int Index { get; set; }
    }

    /// <summary>
    /// 族符号项 - 用于下拉选择
    /// </summary>
    public class FamilySymbolItem2
    {
        public string DisplayName { get; set; }
        public FamilySymbol Symbol { get; set; }
        public int Index { get; set; }
    }

    /// <summary>
    /// 点/线坐标数据模型
    /// </summary>
    public class PointData
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public XYZ ToXYZ() => new XYZ(X, Y, Z);

        public static PointData FromXYZ(XYZ xyz) =>
            xyz == null ? null : new PointData { X = xyz.X, Y = xyz.Y, Z = xyz.Z };
    }
    /// <summary>
    /// 族基础类型枚举
    /// </summary>
    public enum BasedType
    {
        Point,  // 基于点
        Line    // 基于线
    }

    /// <summary>
    /// 基础类型选择ViewModel
    /// </summary>
    public class BasedTypeViewModel : ObserverableObject
    {
        private BasedType _selectedType = BasedType.Point;

        public BasedType SelectedType
        {
            get => _selectedType;
            set { _selectedType = value; OnPropertyChanged(); }
        }

        public bool IsPointBased
        {
            get => _selectedType == BasedType.Point;
            set { if (value) SelectedType = BasedType.Point; }
        }

        public bool IsLineBased
        {
            get => _selectedType == BasedType.Line;
            set { if (value) SelectedType = BasedType.Line; }
        }

        public ICommand NextCommand { get; }
        public ICommand CancelCommand { get; }

        public BasedTypeViewModel()
        {
            NextCommand = new BaseBindingCommand(_ => OnNext());
            CancelCommand = new BaseBindingCommand(_ => CloseWindow?.Invoke());
        }

        private void OnNext()
        {
            TypeSelected?.Invoke(_selectedType);
            CloseWindow?.Invoke();
        }

        public event Action<BasedType> TypeSelected;
        public Action CloseWindow { get; set; }
    }

    /// <summary>
    /// 族实例创建器 - 管理面列表、族符号列表，执行族实例创建
    /// 使用C# 7.3语法：表达式体成员、nameof、元组、模式匹配
    /// </summary>
    public class FamilyInstanceCreator
    {
        #region 私有字段
        private readonly UIDocument _revitDoc;
        private readonly Autodesk.Revit.Creation.Application _appCreator;
        private readonly List<string> _faceNameList = new List<string>();
        private readonly List<Autodesk.Revit.DB.Face> _faceList = new List<Autodesk.Revit.DB.Face>();
        private readonly List<FamilySymbol> _familySymbolList = new List<FamilySymbol>();
        private readonly List<string> _familySymbolNameList = new List<string>();
        private int _defaultIndex = -1;
        #endregion

        #region 公共属性 - 使用表达式体成员
        public IReadOnlyList<string> FaceNameList => _faceNameList;
        public IReadOnlyList<Autodesk.Revit.DB.Face> FaceList => _faceList;
        public IReadOnlyList<FamilySymbol> FamilySymbolList => _familySymbolList;
        public IReadOnlyList<string> FamilySymbolNameList => _familySymbolNameList;
        public int DefaultFamilySymbolIndex => _defaultIndex;
        public UIDocument RevitDoc => _revitDoc;
        #endregion

        #region 构造函数
        /// <summary>
        /// 构造函数 - 检查选中元素并初始化面列表
        /// </summary>
        public FamilyInstanceCreator(UIApplication app)
        {
            _revitDoc = app.ActiveUIDocument ??
                throw new Exception("请打开一个活动文档。");
            _appCreator = app.Application.Create;

            if (!CheckSelectedElementSet())
            {
                throw new Exception("请选中一个具有面几何体的元素（如墙、楼板）。");
            }
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 检查并加载族符号 - 根据基础类型查找或加载默认族
        /// </summary>
        public void CheckFamilySymbol(BasedType type)
        {
            _defaultIndex = -1;
            _familySymbolList.Clear();
            _familySymbolNameList.Clear();

            var defaultSymbolName = type == BasedType.Point ? "Point-based" : "Line-based";

            var collector = new FilteredElementCollector(_revitDoc.Document);
            var familySymbols = collector.OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .ToList();

            bool hasDefaultSymbol = false;
            int index = 0;

            foreach (var symbol in familySymbols)
            {
                if (symbol == null) continue;

                // 检查是否为默认族符号
                if (!hasDefaultSymbol && string.Compare(defaultSymbolName, symbol.Name, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    hasDefaultSymbol = true;
                    _defaultIndex = index;
                }

                _familySymbolList.Add(symbol);
                _familySymbolNameList.Add(FormatSymbolDisplayName(symbol));
                index++;
            }

            // 如果没找到默认族，尝试从文件加载
            if (!hasDefaultSymbol)
            {
                LoadDefaultFamilySymbol(defaultSymbolName);
            }
        }

        /// <summary>
        /// 获取面的边界框 - 用于UI默认值
        /// </summary>
        public BoundingBoxXYZ GetFaceBoundingBox(int faceIndex)
        {
            var mesh = _faceList[faceIndex].Triangulate();
            if (mesh == null) return new BoundingBoxXYZ();

            var min = new XYZ(double.MaxValue, double.MaxValue, double.MaxValue);
            var max = new XYZ(double.MinValue, double.MinValue, double.MinValue);

            foreach (var vertex in mesh.Vertices)
            {
                min = new XYZ(Math.Min(min.X, vertex.X), Math.Min(min.Y, vertex.Y), Math.Min(min.Z, vertex.Z));
                max = new XYZ(Math.Max(max.X, vertex.X), Math.Max(max.Y, vertex.Y), Math.Max(max.Z, vertex.Z));
            }

            return new BoundingBoxXYZ { Min = min, Max = max };
        }

        /// <summary>
        /// 创建基于点的族实例
        /// </summary>
        public bool CreatePointFamilyInstance(XYZ location, XYZ direction, int faceIndex, int familySymbolIndex)
        {
            //var face = _faceList[faceIndex];
            //var symbol = _familySymbolList[familySymbolIndex];

            //if (!symbol.IsActive) symbol.Activate();

            //var instance = _revitDoc.Document.Create.NewFamilyInstance(face, location, direction, symbol);
            //SelectInstance(instance.Id);
            return true;
        }

        /// <summary>
        /// 创建基于线的族实例
        /// </summary>
        public bool CreateLineFamilyInstance(XYZ startPoint, XYZ endPoint, int faceIndex, int familySymbolIndex)
        {
            //var face = _faceList[faceIndex];
            //var symbol = _familySymbolList[familySymbolIndex];

            //// 将点投影到面上
            //var vertices = face.Triangulate()?.Vertices?.ToList() ?? new List<XYZ>();
            //var projectedStart = ProjectToFace(vertices, startPoint);
            //var projectedEnd = ProjectToFace(vertices, endPoint);

            //if (projectedStart.IsAlmostEqualTo(projectedEnd))
            //    return false;

            //if (!symbol.IsActive) symbol.Activate();

            //var line = Autodesk.Revit.DB.Line.CreateBound(projectedStart, projectedEnd);
            //var instance = _revitDoc.Document.Create.NewFamilyInstance(face, line, symbol);
            //SelectInstance(instance.Id);
            return true;
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 检查选中的元素集是否有面几何体
        /// </summary>
        private bool CheckSelectedElementSet()
        {
            var selectedIds = _revitDoc.Selection.GetElementIds();
            if (selectedIds.Count != 1) return false;

            _faceList.Clear();
            _faceNameList.Clear();

            var element = _revitDoc.Document.GetElement(selectedIds.First());
            return element != null && CheckSelectedElement(element);
        }

        /// <summary>
        /// 检查单个元素是否有面几何体
        /// </summary>
        private bool CheckSelectedElement(Element element)
        {
            if (element == null) return false;

            var options = new Options
            {
                View = _revitDoc.Document.ActiveView,
                ComputeReferences = true
            };

            var geometry = element.get_Geometry(options);
            InquireGeometry(geometry, element);

            return _faceList.Count > 0;
        }

        /// <summary>
        /// 递归遍历几何体，收集所有平面
        /// </summary>
        private void InquireGeometry(GeometryElement geometry, Element element)
        {
            if (geometry == null) return;

            var categoryName = element.Category?.Name ?? "未知";

            foreach (var geomObj in geometry)
            {
                switch (geomObj)
                {
                    case GeometryInstance instance:
                        InquireGeometry(instance.SymbolGeometry, element);
                        break;

                    case Solid solid when solid?.Faces != null && !solid.Faces.IsEmpty:
                        CollectFacesFromSolid(solid, categoryName, element.Name);
                        break;
                }
            }
        }

        /// <summary>
        /// 从Solid中收集平面
        /// </summary>
        private void CollectFacesFromSolid(Solid solid, string categoryName, string elementName)
        {
            int faceCounter = 0;
            foreach (Autodesk.Revit.DB.Face face in solid.Faces)
            {
                if (face is PlanarFace)
                {
                    _faceNameList.Add($"{categoryName} : {elementName} ({faceCounter})");
                    _faceList.Add(face);
                    faceCounter++;
                }
            }
        }

        /// <summary>
        /// 格式化族符号显示名称
        /// </summary>
        private string FormatSymbolDisplayName(FamilySymbol symbol)
        {
            var categoryName = symbol.Family.FamilyCategory?.Name;
            var categoryPart = string.IsNullOrEmpty(categoryName) ? "" : $"{categoryName} : ";
            return $"{categoryPart}{symbol.Family.Name} : {symbol.Name}";
        }

        /// <summary>
        /// 加载默认族符号文件
        /// </summary>
        private void LoadDefaultFamilySymbol(string defaultSymbolName)
        {
            try
            {
                string rfaPath = $@"{defaultSymbolName}.rfa";
                bool loaded = _revitDoc.Document.LoadFamilySymbol(rfaPath, defaultSymbolName, out var loadedSymbol);

                if (loaded && loadedSymbol != null)
                {
                    _familySymbolList.Add(loadedSymbol);
                    _familySymbolNameList.Add(FormatSymbolDisplayName(loadedSymbol));
                    _defaultIndex = _familySymbolList.Count - 1;
                }
                else
                {
                    TaskDialog.Show("Revit", $"无法加载预设的族文件: {defaultSymbolName}.rfa");
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Revit", $"加载族文件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 将点投影到面上
        /// </summary>
        private static XYZ ProjectToFace(List<XYZ> vertices, XYZ point)
        {
            if (vertices.Count < 3) return point;

            var a = vertices[0] - vertices[1];
            var b = vertices[0] - vertices[2];
            var c = point - vertices[0];

            var normal = a.CrossProduct(b);
            try
            {
                normal = normal.Normalize();
            }
            catch
            {
                normal = XYZ.Zero;
            }

            return point - normal.DotProduct(c) * normal;
        }

        /// <summary>
        /// 选中刚创建的实例
        /// </summary>
        private void SelectInstance(ElementId instanceId)
        {
            _revitDoc.Selection.SetElementIds(new List<ElementId> { instanceId });
        }
        #endregion
    }
}
