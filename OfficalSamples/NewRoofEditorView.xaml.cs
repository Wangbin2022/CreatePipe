using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace CreatePipe.OfficalSamples
{
    /// <summary>
    /// NewRoofEditorView.xaml 的交互逻辑
    /// </summary>
    public partial class NewRoofEditorView : Window
    {
        public NewRoofEditorView()
        {
            InitializeComponent();
        }
    }
    /// <summary>
    /// 屋顶编辑器ViewModel - 支持PropertyGrid式的属性编辑
    /// </summary>
    public class NewRoofEditorViewModel : ObserverableObject
    {
        private readonly RoofBase _roof;
        private object _selectedWrapper;
        private FootPrintRoofLine _selectedFootPrintLine;

        public ObservableCollection<FootPrintRoofLine> FootPrintLines { get; } = new ObservableCollection<FootPrintRoofLine>();
        public ObservableCollection<RoofType> RoofTypes { get; } = new ObservableCollection<RoofType>();

        public object SelectedWrapper
        {
            get => _selectedWrapper;
            set { _selectedWrapper = value; OnPropertyChanged(); }
        }

        public FootPrintRoofLine SelectedFootPrintLine
        {
            get => _selectedFootPrintLine;
            set
            {
                _selectedFootPrintLine = value;
                OnPropertyChanged();
                UpdateWrapper();
            }
        }

        public RoofType SelectedRoofType
        {
            get => _roof.RoofType;
            set
            {
                if (value != null && value.Id != _roof.RoofType.Id)
                {
                    _roof.RoofType = value;
                    OnPropertyChanged();
                    UpdateWrapper();
                }
            }
        }

        public ICommand OkCommand { get; }
        public ICommand CancelCommand { get; }

        public NewRoofEditorViewModel(ExternalCommandData commandData, RoofBase roof)
        {
            _roof = roof ?? throw new ArgumentNullException(nameof(roof));

            // 加载屋顶类型
            RoofTypes.Clear();
            var collector = new FilteredElementCollector(commandData.Application.ActiveUIDocument.Document);
            foreach (var type in collector.OfClass(typeof(RoofType)).Cast<RoofType>())
                RoofTypes.Add(type);

            // 初始化包装器
            InitializeWrapper();

            OkCommand = new BaseBindingCommand(_ => CloseWindow?.Invoke());
            CancelCommand = new BaseBindingCommand(_ => CloseWindow?.Invoke());
        }

        private void InitializeWrapper()
        {
            switch (_roof)
            {
                case FootPrintRoof fp when fp.GetProfiles() != null:
                    LoadFootPrintLines(fp);
                    SelectedWrapper = new FootPrintRoofWrapper(fp);
                    break;
                case ExtrusionRoof er:
                    SelectedWrapper = new ExtrusionRoofWrapper(er);
                    break;
            }
        }

        private void LoadFootPrintLines(FootPrintRoof roof)
        {
            FootPrintLines.Clear();
            foreach (var curveLoop in roof.GetProfiles().Cast<ModelCurveArray>())
            {
                foreach (var curve in curveLoop.Cast<ModelCurve>())
                {
                    FootPrintLines.Add(new FootPrintRoofLine(roof, curve));
                }
            }
            if (FootPrintLines.Any())
                SelectedFootPrintLine = FootPrintLines.First();
        }

        private void UpdateWrapper()
        {
            //if (_roof is FootPrintRoof fp && SelectedFootPrintLine != null)
            //{
            //    SelectedWrapper = new FootPrintRoofLineWrapper(fp, SelectedFootPrintLine);
            //}
        }

        public Action CloseWindow { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// The ExtrusionRoofWrapper class is use to edit a extrusion roof in a PropertyGrid.
    /// It contains a extrusion roof.
    /// </summary>
    public class ExtrusionRoofWrapper
    {
        // To store the extrusion roof which will be edited in a PropertyGrid.
        private ExtrusionRoof m_roof;

        /// <summary>
        /// The construct of the ExtrusionRoofWrapper class.
        /// </summary>
        /// <param name="roof">The extrusion roof which will be edited in a PropertyGrid.</param>
        public ExtrusionRoofWrapper(ExtrusionRoof roof)
        {
            m_roof = roof;
        }

        #region The properties will be shown in the PropertyGrid
        /// <summary>
        /// The reference plane of the extrusion roof.
        /// </summary>
        [Category("Constrains")]
        [Description("The reference plane of the extrusion roof.")]
        public String WorkPlane
        {
            get
            {
                Parameter para = m_roof.get_Parameter(BuiltInParameter.SKETCH_PLANE_PARAM);
                return para.AsString();
            }
        }

        /// <summary>
        /// The extrusion start point of the extrusion roof.
        /// </summary>
        [Category("Constrains")]
        [DisplayName("Extrusion Start")]
        [Description("The extrusion of a roof can extend in either direction along the reference plane. If the extrusion extends away from the plane, the start and end points are positive values. If the extrusion extends toward the plane, the start and end points are negative.")]
        public String ExtrusionStart
        {
            get
            {
                Parameter para = m_roof.get_Parameter(BuiltInParameter.EXTRUSION_START_PARAM);
                return para.AsValueString();
            }
            set
            {
                Parameter para = m_roof.get_Parameter(BuiltInParameter.EXTRUSION_START_PARAM);
                if (para.SetValueString(value) == false)
                {
                    throw new Exception("Invalid Input");
                }
            }
        }

        /// <summary>
        /// The extrusion end point of the extrusion roof.
        /// </summary>
        [Category("Constrains")]
        [DisplayName("Extrusion End")]
        [Description("The extrusion of a roof can extend in either direction along the reference plane. If the extrusion extends away from the plane, the start and end points are positive values. If the extrusion extends toward the plane, the start and end points are negative.")]
        public String ExtrusionEnd
        {
            get
            {
                Parameter para = m_roof.get_Parameter(BuiltInParameter.EXTRUSION_END_PARAM);
                return para.AsValueString();
            }
            set
            {
                Parameter para = m_roof.get_Parameter(BuiltInParameter.EXTRUSION_END_PARAM);
                if (para.SetValueString(value) == false)
                {
                    throw new Exception("Invalid Input");
                }
            }
        }

        /// <summary>
        /// The reference level of the extrusion roof.
        /// </summary>
        [TypeConverterAttribute(typeof(LevelConverter)), Category("Constrains")]
        [DisplayName("Reference Level")]
        [Description("The reference level of the extrusion roof.")]
        public Level ReferenceLevel
        {
            get
            {
                Parameter para = m_roof.get_Parameter(BuiltInParameter.ROOF_CONSTRAINT_LEVEL_PARAM);
                return LevelConverter.GetLevelByID(para.AsElementId().IntegerValue);
            }
            set
            {
                // update reference level
                Parameter para = m_roof.get_Parameter(BuiltInParameter.ROOF_CONSTRAINT_LEVEL_PARAM);
                Autodesk.Revit.DB.ElementId id = new Autodesk.Revit.DB.ElementId(value.Id.IntegerValue);
                para.Set(id);
            }
        }

        /// <summary>
        /// The offset from the reference level of the extrusion roof.
        /// </summary>
        [Category("Constrains")]
        [DisplayName("Level Offset")]
        [Description("The offset from the reference level.")]
        public String LevelOffset
        {
            get
            {
                Parameter para = m_roof.get_Parameter(BuiltInParameter.ROOF_CONSTRAINT_OFFSET_PARAM);
                return para.AsValueString();
            }
            set
            {
                Parameter para = m_roof.get_Parameter(BuiltInParameter.ROOF_CONSTRAINT_OFFSET_PARAM);
                if (para.SetValueString(value) == false)
                {
                    throw new Exception("Invalid Input");
                }
            }
        }
        #endregion        
    }
    /// <summary>
    /// The Util class is used to translate Revit coordination to windows coordination.
    /// </summary>
    public class Util
    {
        /// <summary>
        /// Translate a Revit 3D point to a windows 2D point according the boundingbox.
        /// </summary>
        /// <param name="pointXYZ">A Revit 3D point</param>
        /// <param name="boundingbox">The boundingbox of the roof whose footprint lines will be displayed in GDI.</param>
        /// <returns>A windows 2D point.</returns>
        static public PointF Translate(Autodesk.Revit.DB.XYZ pointXYZ, BoundingBoxXYZ boundingbox)
        {
            double centerX = (boundingbox.Min.X + boundingbox.Max.X) / 2;
            double centerY = (boundingbox.Min.Y + boundingbox.Max.Y) / 2;
            return new PointF((float)(pointXYZ.X - centerX), -(float)(pointXYZ.Y - centerY));
        }
    };

    /// <summary>
    /// The FootPrintRoofLine class is used to edit the foot print data of a footprint roof. 
    /// </summary>
    public class FootPrintRoofLine
    {
        // To store the footprint roof which the foot print data belong to.
        private FootPrintRoof m_roof;
        // To store the model curve data which the foot print data stand for.
        private ModelCurve m_curve;
        // To store the boundingbox of the roof
        private BoundingBoxXYZ m_boundingbox;
        /// <summary>
        /// The construct of the FootPrintRoofLine class.
        /// </summary>
        /// <param name="roof">The footprint roof which the foot print data belong to.</param>
        /// <param name="curve">The model curve data which the foot print data stand for.</param>
        public FootPrintRoofLine(FootPrintRoof roof, ModelCurve curve)
        {
            m_roof = roof;
            m_curve = curve;
            m_boundingbox = m_roof.get_BoundingBox(roof.Document.ActiveView);
        }

        /// <summary>
        /// Draw the footprint line in GDI.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="pen"></param>
        public void Draw(System.Drawing.Graphics graphics, System.Drawing.Pen pen)
        {
            Curve curve = m_curve.GeometryCurve;
            DrawCurve(graphics, pen, curve);
        }

        /// <summary>
        /// Draw the curve in GDI.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="pen"></param>
        /// <param name="curve"></param>
        private void DrawCurve(Graphics graphics, System.Drawing.Pen pen, Curve curve)
        {
            List<PointF> poinsts = new List<PointF>();
            foreach (Autodesk.Revit.DB.XYZ point in curve.Tessellate())
            {
                poinsts.Add(Util.Translate(point, m_boundingbox));
            }
            graphics.DrawCurve(pen, poinsts.ToArray());
        }

        /// <summary>
        /// Get the model curve data which the foot print data stand for.
        /// </summary>
        [Browsable(false)]
        public ModelCurve ModelCurve
        {
            get
            {
                return m_curve;
            }
        }

        /// <summary>
        /// Get the id value of the model curve.
        /// </summary>
        [Browsable(false)]
        public int Id
        {
            get
            {
                return m_curve.Id.IntegerValue;
            }
        }

        /// <summary>
        /// Get the name of the model curve.
        /// </summary>
        [Browsable(false)]
        public String Name
        {
            get
            {
                return m_curve.Name;
            }
        }

        /// <summary>
        /// Get/Set the slope definition of a model curve of the roof.
        /// </summary>
        [Description("The slope definition of the FootPrintRoof line.")]
        public bool DefinesSlope
        {
            get
            {
                return m_roof.get_DefinesSlope(m_curve);
            }
            set
            {
                m_roof.set_DefinesSlope(m_curve, value);
            }
        }

        /// <summary>
        /// Get/Set the slope angle of the FootPrintRoof line..
        /// </summary>
        [Description("The slope angle of the FootPrintRoof line.")]
        public double SlopeAngle
        {
            get
            {
                return m_roof.get_SlopeAngle(m_curve);
            }
            set
            {
                m_roof.set_SlopeAngle(m_curve, value);
            }
        }

        /// <summary>
        /// Get/Set the offset of the FootPrintRoof line.
        /// </summary>
        [Description("The offset of the FootPrintRoof line.")]
        public double Offset
        {
            get
            {
                return m_roof.get_Offset(m_curve);
            }
            set
            {
                m_roof.set_Offset(m_curve, value);
            }
        }

        /// <summary>
        /// Get/Set the overhang value of the FootPrintRoof line if the roof is created by picked wall.
        /// </summary>
        [Description("The overhang value of the FootPrintRoof line if the roof is created by picked wall.")]
        public double Overhang
        {
            get
            {
                return m_roof.get_Overhang(m_curve);
            }
            set
            {
                m_roof.set_Overhang(m_curve, value);
            }
        }

        /// <summary>
        /// Get/Set ExtendIntoWall value whether you want the overhang to be measured from the core of the wall or not.
        /// </summary>
        [Description("whether you want the overhang to be measured from the core of the wall or not.")]
        public bool ExtendIntoWall
        {
            get
            {
                return m_roof.get_ExtendIntoWall(m_curve);
            }
            set
            {
                m_roof.set_ExtendIntoWall(m_curve, value);
            }
        }
    };

    /// <summary>
    /// The FootPrintRoofWrapper class is use to edit a footprint roof in a PropertyGrid.
    /// It contains a footprint roof.
    /// </summary>
    public class FootPrintRoofWrapper
    {
        // To store the footprint roof which will be edited in a PropertyGrid.
        private FootPrintRoof m_roof;
        // To store the footprint line data of the roof which will be edited.
        private FootPrintRoofLine m_footPrintLine;
        // To store the footprint lines data of the roof.
        private List<FootPrintRoofLine> m_roofLines;

        // To store the boundingbox of the roof
        private BoundingBoxXYZ m_boundingbox;

        public event EventHandler OnFootPrintRoofLineChanged;

        /// <summary>
        /// The construct of the FootPrintRoofWrapper class.
        /// </summary>
        /// <param name="roof">The footprint roof which will be edited in a PropertyGrid.</param>
        public FootPrintRoofWrapper(FootPrintRoof roof)
        {
            m_roof = roof;
            m_roofLines = new List<FootPrintRoofLine>();
            ModelCurveArrArray curveloops = m_roof.GetProfiles();

            foreach (ModelCurveArray curveloop in curveloops)
            {
                foreach (ModelCurve curve in curveloop)
                {
                    m_roofLines.Add(new FootPrintRoofLine(m_roof, curve));
                }
            }

            FootPrintRoofLineConverter.SetStandardValues(m_roofLines);
            m_footPrintLine = m_roofLines[0];

            m_boundingbox = m_roof.get_BoundingBox(roof.Document.ActiveView);
        }


        /// <summary>
        /// Get the bounding box of the roof.
        /// </summary>
        [Browsable(false)]
        public BoundingBoxXYZ Boundingbox
        {
            get
            {
                return m_boundingbox;
            }
        }

        /// <summary>
        /// Get/Set the current footprint roof line which will be edited in the PropertyGrid.
        /// </summary>
        [TypeConverterAttribute(typeof(FootPrintRoofLineConverter)), Category("Footprint Roof Line Information")]
        public FootPrintRoofLine FootPrintLine
        {
            get
            {
                return m_footPrintLine;
            }
            set
            {
                m_footPrintLine = value;
                OnFootPrintRoofLineChanged(this, new EventArgs());
            }
        }

        /// <summary>
        /// The base level of the footprint roof.
        /// </summary>
        [TypeConverterAttribute(typeof(LevelConverter)), Category("Constrains")]
        [DisplayName("Base Level")]
        public Level BaseLevel
        {
            get
            {
                Parameter para = m_roof.get_Parameter(BuiltInParameter.ROOF_BASE_LEVEL_PARAM);
                return LevelConverter.GetLevelByID(para.AsElementId().IntegerValue);
            }
            set
            {
                // update base level
                Parameter para = m_roof.get_Parameter(BuiltInParameter.ROOF_BASE_LEVEL_PARAM);
                Autodesk.Revit.DB.ElementId id = new Autodesk.Revit.DB.ElementId(value.Id.IntegerValue);
                para.Set(id);
            }
        }

        /// <summary>
        /// The eave cutter type of the footprint roof.
        /// </summary>
        [Category("Construction")]
        [DisplayName("Rafter Cut")]
        [Description("The eave cutter type of the footprint roof.")]
        public EaveCutterType EaveCutterType
        {
            get
            {
                return m_roof.EaveCuts;
            }
            set
            {
                m_roof.EaveCuts = value;
            }
        }

        /// <summary>
        /// Get the footprint roof lines data.
        /// </summary>
        [Browsable(false)]
        public ReadOnlyCollection<FootPrintRoofLine> FootPrintRoofLines
        {
            get
            {
                return new ReadOnlyCollection<FootPrintRoofLine>(m_roofLines);
            }
        }

        /// <summary>
        /// Draw the footprint lines.
        /// </summary>
        /// <param name="graphics">The graphics object.</param>
        /// <param name="displayPen">A display pen.</param>
        /// <param name="highlightPen">A highlight pen.</param>
        public void DrawFootPrint(Graphics graphics, System.Drawing.Pen displayPen, System.Drawing.Pen highlightPen)
        {
            foreach (FootPrintRoofLine line in m_roofLines)
            {
                if (line.Id == m_footPrintLine.Id)
                {
                    line.Draw(graphics, highlightPen);
                }
                else
                {
                    line.Draw(graphics, displayPen);
                }
            }
        }
    }

    /// <summary>
    /// The LevelConverter class is inherited from the TypeConverter class which is used to
    /// show the property which returns Level type as like a combo box in the PropertyGrid control.
    /// </summary>
    public class LevelConverter : TypeConverter
    {
        /// <summary>
        /// To store the levels element
        /// </summary>
        static private Dictionary<String, Level> m_levels = new Dictionary<String, Level>();

        /// <summary>
        /// Initialize the levels data.
        /// </summary>
        /// <param name="levels"></param>
        static public void SetStandardValues(ReadOnlyCollection<Level> levels)
        {
            m_levels.Clear();
            foreach (Level level in levels)
            {
                m_levels.Add(level.Id.IntegerValue.ToString(), level);
            }
        }

        /// <summary>
        /// Get a level by a level id.
        /// </summary>
        /// <param name="id">The id of the level</param>
        /// <returns>Returns a level which id equals the specified id.</returns>
        static public Level GetLevelByID(int id)
        {
            return m_levels[id.ToString()];
        }

        /// <summary>
        /// Override the CanConvertTo method.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="destinationType"></param>
        /// <returns></returns>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(Level))
                return true;
            return base.CanConvertTo(context, destinationType);
        }

        /// <summary>
        ///  Override the ConvertTo method, convert a level type value to a string type value for displaying in the PropertyGrid.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="culture"></param>
        /// <param name="value"></param>
        /// <param name="destinationType"></param>
        /// <returns></returns>
        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(String) && value is Level)
            {
                Level level = (Level)value;
                return level.Name + "[" + level.Id.IntegerValue.ToString() + "]";
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// <summary>
        /// Override the CanConvertFrom method.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="sourceType"></param>
        /// <returns></returns>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(String))
                return true;
            return base.CanConvertFrom(context, sourceType);
        }

        /// <summary>
        /// Override the ConvertFrom method, convert a string type value to a level type value.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="culture"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if (value is String)
            {
                try
                {
                    String levelString = (String)value;

                    int leftBracket = levelString.IndexOf('[');
                    int rightBracket = levelString.IndexOf(']');

                    String idString = levelString.Substring(leftBracket + 1, rightBracket - leftBracket - 1);

                    return m_levels[idString];
                }
                catch (Exception ex)
                {
                    Autodesk.Revit.UI.TaskDialog.Show("Revit", ex.Message);
                }
            }
            return base.ConvertFrom(context, culture, value);
        }

        /// <summary>
        /// Override the GetStandardValuesSupported method for displaying a level list in the PropertyGrid.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        /// <summary>
        /// Override the StandardValuesCollection method for supplying a level list in the PropertyGrid.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return new StandardValuesCollection(m_levels.Values);
        }
    }

    /// <summary>
    /// The FootPrintRoofLineConverter class is inherited from the ExpandableObjectConverter class which is used to
    /// expand the property which returns FootPrintRoofLine type as like a tree view in the PropertyGrid control.
    /// </summary>
    public class FootPrintRoofLineConverter : ExpandableObjectConverter
    {
        // To store the FootPrintRoofLines data.
        static private Dictionary<String, FootPrintRoofLine> m_footPrintLines = new Dictionary<String, FootPrintRoofLine>();

        /// <summary>
        /// Initialize the FootPrintRoofLines data. 
        /// </summary>
        /// <param name="footPrintRoofLines"></param>
        static public void SetStandardValues(List<FootPrintRoofLine> footPrintRoofLines)
        {
            m_footPrintLines.Clear();
            foreach (FootPrintRoofLine footPrintLine in footPrintRoofLines)
            {
                if (m_footPrintLines.ContainsKey(footPrintLine.Id.ToString()))
                    continue;
                m_footPrintLines.Add(footPrintLine.Id.ToString(), footPrintLine);
            }
        }

        /// <summary>
        /// Override the CanConvertTo method.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="sourceType"></param>
        /// <returns></returns>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(FootPrintRoofLine))
                return true;
            return base.CanConvertTo(context, destinationType);
        }

        /// <summary>
        ///  Override the ConvertTo method, convert a FootPrintRoofLine type value to a string type value for displaying in the PropertyGrid.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="culture"></param>
        /// <param name="value"></param>
        /// <param name="destinationType"></param>
        /// <returns></returns>
        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(System.String) && value is FootPrintRoofLine)
            {
                FootPrintRoofLine footPrintLine = (FootPrintRoofLine)value;
                return footPrintLine.Name + "[" + footPrintLine.Id.ToString() + "]";
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// <summary>
        /// Override the CanConvertFrom method.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="sourceType"></param>
        /// <returns></returns>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
                return true;
            return base.CanConvertFrom(context, sourceType);
        }

        /// <summary>
        /// Override the ConvertFrom method, convert a string type value to a FootPrintRoofLine type value.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="culture"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if (value is String)
            {
                try
                {
                    String footPrintLineString = (String)value;

                    int leftBracket = footPrintLineString.IndexOf('[');
                    int rightBracket = footPrintLineString.IndexOf(']');

                    String idString = footPrintLineString.Substring(leftBracket + 1, rightBracket - leftBracket - 1);

                    return m_footPrintLines[idString];
                }
                catch (Exception ex)
                {
                    Autodesk.Revit.UI.TaskDialog.Show("Revit", ex.Message);
                }
            }

            return base.ConvertFrom(context, culture, value);
        }

        /// <summary>
        /// Override the GetStandardValuesSupported method for displaying a FootPrintRoofLine list in the PropertyGrid.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        /// <summary>
        /// Override the StandardValuesCollection method for supplying a FootPrintRoofLine list in the PropertyGrid.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return new StandardValuesCollection(m_footPrintLines.Values);
        }
    };
}
