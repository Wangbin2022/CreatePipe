using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using CreatePipe.Obselete;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
    /// ZoneEditorWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ZoneEditorWindow : Window
    {
        public ZoneEditorWindow()
        {
            InitializeComponent();
        }
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
    public class ZoneEditorMainViewModel : ObserverableObject
    {
        private readonly DataManager _dataManager;
        private Level _currentLevel;
        private ObservableCollection<Level> _levels;
        private ObservableCollection<SpaceItem> _spaces;
        private ObservableCollection<ZoneNode> _zones;
        private ZoneNode _selectedZone;

        public ZoneEditorMainViewModel(ExternalCommandData commandData)
        {
            _dataManager = new DataManager(commandData);
            Levels = new ObservableCollection<Level>(_dataManager.Levels);
            CurrentLevel = Levels.FirstOrDefault();

            CreateSpacesCommand = new RelayCommand(_ => CreateSpaces(), _ => CurrentLevel != null);
            CreateZoneCommand = new RelayCommand(_ => CreateZone(), _ => CurrentLevel != null);
            EditZoneCommand = new RelayCommand(_ => EditZone(), _ => SelectedZone != null);
        }

        public ObservableCollection<Level> Levels
        {
            get => _levels;
            set { _levels = value; OnPropertyChanged(); }
        }

        public Level CurrentLevel
        {
            get => _currentLevel;
            set
            {
                _currentLevel = value;
                OnPropertyChanged();
                _dataManager.Update(value);
                RefreshData();
            }
        }

        public ObservableCollection<SpaceItem> Spaces
        {
            get => _spaces;
            set { _spaces = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ZoneNode> Zones
        {
            get => _zones;
            set { _zones = value; OnPropertyChanged(); }
        }

        public ZoneNode SelectedZone
        {
            get => _selectedZone;
            set { _selectedZone = value; OnPropertyChanged(); }
        }

        public ICommand CreateSpacesCommand { get; }
        public ICommand CreateZoneCommand { get; }
        public ICommand EditZoneCommand { get; }

        private void RefreshData()
        {
            RefreshSpaces();
            RefreshZones();
        }

        private void RefreshSpaces()
        {
            var spaces = _dataManager.GetSpaces();
            Spaces = new ObservableCollection<SpaceItem>(spaces.Select(s => new SpaceItem(s)));
        }

        private void RefreshZones()
        {
            var zones = _dataManager.GetZones();
            Zones = new ObservableCollection<ZoneNode>(zones.Select(z => new ZoneNode(z)));
        }

        private void CreateSpaces()
        {
            _dataManager.CreateSpaces();
            RefreshSpaces();
        }

        private void CreateZone()
        {
            _dataManager.CreateZone();
            RefreshZones();
        }

        private void EditZone()
        {
            if (SelectedZone == null) return;

            var editorVm = new ZoneEditorViewModel(_dataManager, SelectedZone.Zone);
            var editorWindow = new ZoneEditorWindow { DataContext = editorVm };

            if (editorWindow.ShowDialog() == true)
            {
                RefreshSpaces();
                RefreshZones();
            }
        }
    }
    public class SpaceItem : ObserverableObject
    {
        private readonly Space _space;
        private string _zoneName;
        public SpaceItem(Space space)
        {
            _space = space;
            _zoneName = space.Zone?.Name ?? "Default";
        }
        public string Name => _space.Name;
        public string ZoneName
        {
            get => _zoneName;
            set { _zoneName = value; OnPropertyChanged(); }
        }
        public Space Space => _space;
        public void UpdateZone(Zone zone) => ZoneName = zone?.Name ?? "Default";
    }
    public class ZoneNode : ObserverableObject
    {
        private readonly Zone _zone;
        private ObservableCollection<SpaceItem> _spaces;

        public ZoneNode(Zone zone)
        {
            _zone = zone;
            _spaces = new ObservableCollection<SpaceItem>();
        }

        public string Name => _zone.Name;
        public string Phase => _zone.Phase?.Name ?? "Unknown";
        public Zone Zone => _zone;
        public ObservableCollection<SpaceItem> Spaces
        {
            get => _spaces;
            set { _spaces = value; OnPropertyChanged(); }
        }
    }
    public class ZoneEditorViewModel : ObserverableObject
    {
        private readonly DataManager _dataManager;
        private readonly Zone _zone;
        private ObservableCollection<SpaceItem> _availableSpaces;
        private ObservableCollection<SpaceItem> _zoneSpaces;

        public ZoneEditorViewModel(DataManager dataManager, Zone zone)
        {
            _dataManager = dataManager;
            _zone = zone;

            LoadSpaces();

            AddSpaceCommand = new RelayCommand(_ => MoveSpace(true), _ => SelectedAvailableSpace != null);
            RemoveSpaceCommand = new RelayCommand(_ => MoveSpace(false), _ => SelectedZoneSpace != null);
            OKCommand = new RelayCommand(_ => ApplyChanges());
        }

        private void LoadSpaces()
        {
            var allSpaces = _dataManager.GetSpaces();
            //var zoneSpaceIds = _zone.GetSpaces().Cast<Space>().Select(s => s.Id).ToHashSet();
            var zoneSpaces = _zone.Spaces;
            var zoneSpaceIds = zoneSpaces.Cast<Space>().Select(s => s.Id).ToHashSet();

            AvailableSpaces = new ObservableCollection<SpaceItem>(
                allSpaces.Where(s => !zoneSpaceIds.Contains(s.Id)).Select(s => new SpaceItem(s)));

            ZoneSpaces = new ObservableCollection<SpaceItem>(
                allSpaces.Where(s => zoneSpaceIds.Contains(s.Id)).Select(s => new SpaceItem(s)));
        }

        public ObservableCollection<SpaceItem> AvailableSpaces
        {
            get => _availableSpaces;
            set { _availableSpaces = value; OnPropertyChanged(); }
        }

        public ObservableCollection<SpaceItem> ZoneSpaces
        {
            get => _zoneSpaces;
            set { _zoneSpaces = value; OnPropertyChanged(); }
        }

        public SpaceItem SelectedAvailableSpace { get; set; }
        public SpaceItem SelectedZoneSpace { get; set; }

        public ICommand AddSpaceCommand { get; }
        public ICommand RemoveSpaceCommand { get; }
        public ICommand OKCommand { get; }

        private void MoveSpace(bool addToZone)
        {
            if (addToZone && SelectedAvailableSpace != null)
            {
                ZoneSpaces.Add(SelectedAvailableSpace);
                AvailableSpaces.Remove(SelectedAvailableSpace);
            }
            else if (!addToZone && SelectedZoneSpace != null)
            {
                AvailableSpaces.Add(SelectedZoneSpace);
                ZoneSpaces.Remove(SelectedZoneSpace);
            }
        }

        private void ApplyChanges()
        {
            using (var trans = new Transaction(_dataManager.Document, "Edit Zone"))
            {
                trans.Start();
                var zoneSpaces = _zone.Spaces;
                var currentSpaceIds = zoneSpaces.Cast<Space>().Select(s => s.Id).ToHashSet();
                //var currentSpaceIds = _zone.GetSpaces().Cast<Space>().Select(s => s.Id).ToHashSet();
                var newSpaceIds = ZoneSpaces.Select(s => s.Space.Id).ToHashSet();

                var toAdd = newSpaceIds.Except(currentSpaceIds).ToList();
                var toRemove = currentSpaceIds.Except(newSpaceIds).ToList();

                if (toAdd.Any())
                {
                    var spaceSet = new SpaceSet();
                    toAdd.ForEach(id => spaceSet.Insert(_dataManager.Document.GetElement(id) as Space));
                    _zone.AddSpaces(spaceSet);
                }

                if (toRemove.Any())
                {
                    var spaceSet = new SpaceSet();
                    toRemove.ForEach(id => spaceSet.Insert(_dataManager.Document.GetElement(id) as Space));
                    _zone.RemoveSpaces(spaceSet);
                }

                trans.Commit();
            }
        }
    }
    /// <summary>
    /// The DataManager Class is used to obtain, create or edit the Space elements and Zone elements.
    /// </summary>
    public class DataManager
    {
        // 在DataManager类中添加以下属性
        public Document Document => m_commandData.Application.ActiveUIDocument.Document;

        // 修改GetSpaces和GetZones方法返回类型
        public List<Space> GetSpaces() => m_spaceManager.GetSpaces(m_currentLevel);
        public List<Zone> GetZones() => m_zoneManager.GetZones(m_currentLevel);

        // 修改CreateSpaces和CreateZone为无参方法
        public void CreateSpaces() => CreateSpaces();
        public void CreateZone() => CreateZone();

        ExternalCommandData m_commandData;
        List<Level> m_levels;
        SpaceManager m_spaceManager;
        ZoneManager m_zoneManager;
        Level m_currentLevel;
        Phase m_defaultPhase;

        /// <summary>
        /// The constructor of DataManager class.
        /// </summary>
        /// <param name="commandData">The ExternalCommandData</param>
        public DataManager(ExternalCommandData commandData)
        {
            m_commandData = commandData;
            m_levels = new List<Level>();
            Initialize();
            m_currentLevel = m_levels[0];
            Parameter para = commandData.Application.ActiveUIDocument.Document.ActiveView.get_Parameter(Autodesk.Revit.DB.BuiltInParameter.VIEW_PHASE);
            Autodesk.Revit.DB.ElementId phaseId = para.AsElementId();
            m_defaultPhase = commandData.Application.ActiveUIDocument.Document.GetElement(phaseId) as Phase;
        }

        /// <summary>
        /// Initialize the data member, obtain the Space and Zone elements.
        /// </summary>
        private void Initialize()
        {
            Dictionary<int, List<Space>> spaceDictionary = new Dictionary<int, List<Space>>();
            Dictionary<int, List<Zone>> zoneDictionary = new Dictionary<int, List<Zone>>();

            Document activeDoc = m_commandData.Application.ActiveUIDocument.Document;

            FilteredElementIterator levelsIterator = (new FilteredElementCollector(activeDoc)).OfClass(typeof(Level)).GetElementIterator();
            FilteredElementIterator spacesIterator = (new FilteredElementCollector(activeDoc)).WherePasses(new SpaceFilter()).GetElementIterator();
            FilteredElementIterator zonesIterator = (new FilteredElementCollector(activeDoc)).OfClass(typeof(Zone)).GetElementIterator();

            levelsIterator.Reset();
            while (levelsIterator.MoveNext())
            {
                Level level = levelsIterator.Current as Level;
                if (level != null)
                {
                    m_levels.Add(level);
                    spaceDictionary.Add(level.Id.IntegerValue, new List<Space>());
                    zoneDictionary.Add(level.Id.IntegerValue, new List<Zone>());
                }
            }

            spacesIterator.Reset();
            while (spacesIterator.MoveNext())
            {
                Space space = spacesIterator.Current as Space;
                if (space != null)
                {
                    spaceDictionary[space.LevelId.IntegerValue].Add(space);
                }
            }

            zonesIterator.Reset();
            while (zonesIterator.MoveNext())
            {
                Zone zone = zonesIterator.Current as Zone;
                if (zone != null && activeDoc.GetElement(zone.LevelId) != null)
                {
                    zoneDictionary[zone.LevelId.IntegerValue].Add(zone);
                }
            }

            m_spaceManager = new SpaceManager(m_commandData, spaceDictionary);
            m_zoneManager = new ZoneManager(m_commandData, zoneDictionary);
        }

        /// <summary>
        /// Get the Level elements.
        /// </summary>
        public ReadOnlyCollection<Level> Levels
        {
            get
            {
                return new ReadOnlyCollection<Level>(m_levels);
            }
        }

        ///// <summary>
        ///// Create a Zone element.
        ///// </summary>
        //public void CreateZone()
        //{
        //    if (m_defaultPhase == null)
        //    {
        //        Autodesk.Revit.UI.TaskDialog.Show("Revit", "The phase of the active view is null, you can't create zone in a null phase");
        //        return;
        //    }
        //    try
        //    {
        //        this.m_zoneManager.CreateZone(m_currentLevel, m_defaultPhase);
        //    }
        //    catch (Exception ex)
        //    {
        //        Autodesk.Revit.UI.TaskDialog.Show("Revit", ex.Message);
        //    }
        //}

        ///// <summary>
        ///// Create some spaces.
        ///// </summary>
        //public void CreateSpaces()
        //{
        //    if (m_defaultPhase == null)
        //    {
        //        Autodesk.Revit.UI.TaskDialog.Show("Revit", "The phase of the active view is null, you can't create spaces in a null phase");
        //        return;
        //    }

        //    try
        //    {
        //        if (m_commandData.Application.ActiveUIDocument.Document.ActiveView.ViewType == Autodesk.Revit.DB.ViewType.FloorPlan)
        //        {
        //            m_spaceManager.CreateSpaces(m_currentLevel, m_defaultPhase);
        //        }
        //        else
        //        {
        //            Autodesk.Revit.UI.TaskDialog.Show("Revit", "You can not create spaces in this plan view");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Autodesk.Revit.UI.TaskDialog.Show("Revit", ex.Message);
        //    }
        //}

        ///// <summary>
        ///// Get the Space elements.
        ///// </summary>
        ///// <returns>A space list in current level.</returns>
        //public List<Space> GetSpaces()
        //{
        //    return m_spaceManager.GetSpaces(m_currentLevel);
        //}

        ///// <summary>
        ///// Get the Zone elements.
        ///// </summary>
        ///// <returns>A Zone list in current level.</returns>
        //public List<Zone> GetZones()
        //{
        //    return m_zoneManager.GetZones(m_currentLevel);
        //}

        /// <summary>
        /// Update the current level.
        /// </summary>
        /// <param name="level"></param>
        public void Update(Level level)
        {
            this.m_currentLevel = level;
        }
    }
    /// <summary>
    /// The SpaceManager class is used to manage the Spaces elements in the current document.
    /// </summary>
    class SpaceManager
    {
        ExternalCommandData m_commandData;
        Dictionary<int, List<Space>> m_spaceDictionary;

        /// <summary>
        /// The constructor of SpaceManager class.
        /// </summary>
        /// <param name="data">The ExternalCommandData</param>
        /// <param name="spaceData">The spaceData contains all the Space elements in different level.</param>
        public SpaceManager(ExternalCommandData data, Dictionary<int, List<Space>> spaceData)
        {
            m_commandData = data;
            m_spaceDictionary = spaceData;
        }

        /// <summary>
        /// Get the Spaces elements in a specified level.
        /// </summary>
        /// <param name="level"></param>
        /// <returns>Return a space list</returns>
        public List<Space> GetSpaces(Level level)
        {
            return m_spaceDictionary[level.Id.IntegerValue];
        }

        /// <summary>
        /// Create the space for each closed wall loop or closed space separation in the active view.
        /// </summary>
        /// <param name="level">The level in which the spaces is to exist.</param>
        /// <param name="phase">The phase in which the spaces is to exist.</param>
        public void CreateSpaces(Level level, Phase phase)
        {
            try
            {
                ICollection<ElementId> elements = m_commandData.Application.ActiveUIDocument.Document.Create.NewSpaces2(level, phase, this.m_commandData.Application.ActiveUIDocument.Document.ActiveView);
                foreach (ElementId elem in elements)
                {
                    Space space = m_commandData.Application.ActiveUIDocument.Document.GetElement(elem) as Space;
                    if (space != null)
                    {
                        m_spaceDictionary[level.Id.IntegerValue].Add(space);
                    }
                }
                if (elements == null || elements.Count == 0)
                {
                    Autodesk.Revit.UI.TaskDialog.Show("Revit", "There is no enclosed loop in " + level.Name);
                }

            }
            catch (Exception ex)
            {
                Autodesk.Revit.UI.TaskDialog.Show("Revit", ex.Message);
            }
        }
    }
    /// <summary>
    /// The ZoneManager class is used to manage the Zone elements in the current document.
    /// </summary>
    class ZoneManager
    {
        ExternalCommandData m_commandData;
        Dictionary<int, List<Zone>> m_zoneDictionary;
        Level m_currentLevel;
        Zone m_currentZone;

        /// <summary>
        /// The constructor of ZoneManager class.
        /// </summary>
        /// <param name="commandData">The ExternalCommandData</param>
        /// <param name="zoneData">The spaceData contains all the Zone elements in different level.</param>
        public ZoneManager(ExternalCommandData commandData, Dictionary<int, List<Zone>> zoneData)
        {
            m_commandData = commandData;
            m_zoneDictionary = zoneData;
        }

        /// <summary>
        /// Create a zone in a specified level and phase.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="phase"></param>
        public void CreateZone(Level level, Phase phase)
        {
            Zone zone = m_commandData.Application.ActiveUIDocument.Document.Create.NewZone(level, phase);
            if (zone != null)
            {
                this.m_zoneDictionary[level.Id.IntegerValue].Add(zone);
            }
        }

        /// <summary>
        /// Add some spaces to current Zone.
        /// </summary>
        /// <param name="spaces"></param>
        public void AddSpaces(SpaceSet spaces)
        {
            m_currentZone.AddSpaces(spaces);
        }

        /// <summary>
        /// Remove some spaces to current Zone.
        /// </summary>
        /// <param name="spaces"></param>
        public void RemoveSpaces(SpaceSet spaces)
        {
            m_currentZone.RemoveSpaces(spaces);
        }

        /// <summary>
        /// Get the Zone elements in a specified level.
        /// </summary>
        /// <param name="level"></param>
        /// <returns>Return a zone list</returns>
        public List<Zone> GetZones(Level level)
        {
            m_currentLevel = level;
            return m_zoneDictionary[level.Id.IntegerValue];
        }

        /// <summary>
        /// Get/Set the Current Zone element.
        /// </summary>
        public Zone CurrentZone
        {
            get
            {
                return CurrentZone;
            }
            set
            {
                m_currentZone = value;
            }
        }
    }

}
