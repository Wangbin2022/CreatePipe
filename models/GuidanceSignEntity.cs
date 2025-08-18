using Autodesk.Revit.DB;
using CreatePipe.cmd;
using CreatePipe.Utils;

namespace CreatePipe.models
{
    public class GuidanceSignEntity : ObserverableObject
    {
        Document Document { get; set; }
        private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
        public GuidanceSignEntity(IndependentTag tag)
        {
            Document = tag.Document;

            Id = tag.Id;

            entityId = tag.TaggedLocalElementId;
            FamilyInstance entity = Document.GetElement(entityId) as FamilyInstance;
            entityName = entity.Name;
            entityLevelName = Document.GetElement(entity.LevelId).Name;
            entityContent= entity.LookupParameter("标识内容").AsString(); 

            locationCode = entity.Symbol.LookupParameter("位置编码").AsString();
            levelCode = entity.LookupParameter("层高编码").AsString();
            typeCode = entity.Symbol.LookupParameter("性质编码").AsString();
            serialCode = entity.LookupParameter("本层编号").AsString();
            installCode = entity.LookupParameter("悬挂方式编码").AsString();

            //tagName = tag.TagText;
            //直接取值不可靠，最好通过字符串组合
            tagName = locationCode+levelCode+"-"+typeCode+"-"+serialCode;
        }

        public ElementId entityId { get; set; }
        public string entityName { get; set; }
        public string entityLevelName { get; set; }
        public string entityContent { get; set; }

        public string signLevelName { get; set; }
        public string installCode { get; set; }
        public string serialCode { get; set; }
        public string typeCode { get; set; }
        public string levelCode { get; set; }
        public string locationCode { get; set; }
        public string tagName { get; set; }
        public ElementId Id { get; set; }
    }
}
