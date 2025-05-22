using Autodesk.Revit.DB;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe.NCCoding
{
    //0522 如何区分系统族和可载入族
    public class NCCodingEntity : ObserverableObject
    {
        public NCCodingEntity(Object obj)
        {
            if (obj is FamilyInstance)
            {

            }
        }
        private bool _isCompliant = false;
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
        public List<Family> FamilyCollection { get; set; } = new List<Family> { };
        public int FamilyCount { get; }
        public int CompliantFamilyCount { get; }
        public ElementId CategoryId { get; }
        public string FamilyName { get; set; }
    }
}
