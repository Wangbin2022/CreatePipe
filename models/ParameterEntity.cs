using Autodesk.Revit.DB;
using CreatePipe.cmd;

namespace CreatePipe.models
{
    public class ParameterEntity : ObserverableObject
    {
        public string Name { get; set; }
        public BuiltInParameter BuiltInId { get; set; }
        public ElementId Id { get; set; } // 用于匹配自定义参数
        public bool IsBuiltIn { get; set; }
        public ParameterEntity(Parameter param)
        {
            Name = param.Definition.Name;
            IsBuiltIn = param.IsShared == false && param.Id.IntegerValue < 0;
            BuiltInId = IsBuiltIn ? (BuiltInParameter)param.Id.IntegerValue : BuiltInParameter.INVALID;
            Id = param.Id;
        }

        //用于外部比较是否ID匹配和筛选唯一
        public override bool Equals(object obj)
        {
            if (obj is ParameterEntity other)
                return IsBuiltIn ? this.BuiltInId == other.BuiltInId : this.Id == other.Id;
            return false;
        }
        public override int GetHashCode() => IsBuiltIn ? BuiltInId.GetHashCode() : Id.GetHashCode();
    }
}
