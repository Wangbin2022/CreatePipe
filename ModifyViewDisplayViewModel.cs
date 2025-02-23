using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CreatePipe.cmd;
using CreatePipe.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;



namespace CreatePipe
{
    public class ModifyViewDisplayViewModel : ObserverableObject
    {
        public Document Doc { get; set; }
        public ModifyViewDisplayViewModel(UIApplication application)
        {
            Doc = application.ActiveUIDocument.Document;
            SelectLine = displine.FirstOrDefault();
            SelectDetail = displayDetail.FirstOrDefault();
            selectStyle = displayStyle.FirstOrDefault();
        }
        public ICommand ModifyViewCommand => new RelayCommand<string>(ModifyView);
        private void ModifyView(string obj)
        {
            //TaskDialog.Show("tt", $"{SelectLine}+{SelectDetail}+{SelectStyle}+{obj}");
            List<View> views = new List<View>();
            if (obj == "A")
            {
                views = new FilteredElementCollector(Doc).OfClass(typeof(View)).Cast<View>().Where(view => view.GetType().Name == "ViewPlan" || view.GetType().Name == "ViewSection").ToList();
            }
            else
            {
                views = new FilteredElementCollector(Doc).OfClass(typeof(View)).Cast<View>().Where(view => view.GetType().Name == "View3D").ToList();
            }
            XmlDoc.Instance.Task.Run(app =>
            {
                Doc.NewTransaction(() =>
                {
                    foreach (var view in views)
                    {
                        if (!view.IsTemplate)
                        {
                            view.ViewTemplateId = ElementId.InvalidElementId;
                            switch (SelectLine)
                            {
                                case "协调":
                                    view.Discipline = ViewDiscipline.Coordination;
                                    break;
                                case "建筑":
                                    view.Discipline = ViewDiscipline.Architectural;
                                    break;
                                case "结构":
                                    view.Discipline = ViewDiscipline.Structural;
                                    break;
                                case "机械":
                                    view.Discipline = ViewDiscipline.Mechanical;
                                    break;
                                case "电气":
                                    view.Discipline = ViewDiscipline.Electrical;
                                    break;
                                default:
                                    view.Discipline = ViewDiscipline.Plumbing;
                                    break;
                            }
                            switch (SelectDetail)
                            {
                                case "粗略":
                                    view.DetailLevel = ViewDetailLevel.Coarse;
                                    break;
                                case "中等":
                                    view.DetailLevel = ViewDetailLevel.Medium;
                                    break;
                                default:
                                    view.DetailLevel = ViewDetailLevel.Fine;
                                    break;
                            }
                            switch (selectStyle)
                            {
                                case "线框":
                                    view.DisplayStyle = DisplayStyle.Wireframe;
                                    break;
                                case "隐藏线":
                                    view.DisplayStyle = DisplayStyle.HLR;
                                    break;
                                case "着色":
                                    view.DisplayStyle = DisplayStyle.ShadingWithEdges;
                                    break;
                                case "一致的颜色":
                                    view.DisplayStyle = DisplayStyle.Shading;
                                    break;
                                default:
                                    view.DisplayStyle = DisplayStyle.Realistic;
                                    break;
                            }
                        }
                    }
                }, "设置视图显示");
            });
        }
        public string SelectLine
        {
            get => selectLine;
            set
            {
                selectLine = value;
                OnPropertyChanged();
            }
        }
        public string SelectDetail
        {
            get => selectDetail;
            set
            {
                selectDetail = value;
                OnPropertyChanged();
            }
        }
        public string SelectStyle
        {
            get => selectStyle;
            set
            {
                selectStyle = value;
                OnPropertyChanged();
            }
        }
        private string selectStyle;
        private string selectDetail;
        private string selectLine;
        public List<string> displine { get; set; } = new List<string> { "协调", "建筑", "结构", "机械", "电气", "卫浴" };
        public List<string> displayDetail { get; set; } = new List<string> { "粗略", "中等", "精细" };
        public List<string> displayStyle { get; set; } = new List<string> { "线框", "隐藏线", "着色", "一致的颜色", "真实" };
    }
}
