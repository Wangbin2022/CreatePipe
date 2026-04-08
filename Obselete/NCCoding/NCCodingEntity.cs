using Autodesk.Revit.DB;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CreatePipe.NCCoding
{
    //0522 如何区分系统族和可载入族
    public class NCCodingEntity : ObserverableObject
    {
        public Document Document { get; set; }
        public NCCodingEntity(Object obj)
        {
            if (obj is Family family)
            {
                Document = family.Document;
                FamilyName = family.Name;
                CategoryId = ((FamilySymbol)Document.GetElement(family.GetFamilySymbolIds().FirstOrDefault())).Category.Id;
                CategoryName = Category.GetCategory(Document, CategoryId).Name;
                var allFamilyInstances = new FilteredElementCollector(Document).OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>();
                foreach (var item in allFamilyInstances)
                {
                    if (item.Symbol.FamilyName == FamilyName)
                    {
                        FamilyCollection.Add(item);
                    }
                }
                FamilyCount = FamilyCollection.Count();
                foreach (var item in FamilyCollection)
                {
                    Parameter parameter = item.LookupParameter("族ID");
                    if (parameter == null || string.IsNullOrWhiteSpace(parameter.AsString()))
                    {
                        IsCompliant = false;
                        canCode = true;
                    }
                    else
                    {
                        projectId = item.LookupParameter("族ID").AsString();
                        CompliantFamilyCount++;
                    }
                }
            }
            else
            {
                return;
            }
        }
        public string projectId { get; set; }
        public bool canCode { get; set; } = false;
        private bool _isCompliant = true;
        public bool IsCompliant
        {
            get => _isCompliant;
            set
            {
                if (_isCompliant != value)
                {
                    _isCompliant = value;
                    OnPropertyChanged(nameof(IsCompliant));
                    //// 触发合规状态变更事件（可选）
                    //ComplianceStatusChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        public List<FamilyInstance> FamilyCollection { get; set; } = new List<FamilyInstance>();
        public int FamilyCount { get; }
        public int CompliantFamilyCount { get; } = 0;
        public ElementId CategoryId { get; }
        public string CategoryName { get; }
        public string FamilyName { get; set; }
    }
}
