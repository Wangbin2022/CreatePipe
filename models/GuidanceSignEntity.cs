using Autodesk.Revit.DB;
using CreatePipe.cmd;
using CreatePipe.Utils;
using System;
using System.Linq;

namespace CreatePipe.models
{
    public class GuidanceSignEntity : ObserverableObject
    {
        private Document _document { get; set; }
        private readonly BaseExternalHandler _externalHandler = new BaseExternalHandler();
        public FamilyInstance Entity { get; private set; }
        public ElementId EntityId { get; private set; }
        public ElementId Id { get; private set; }

        private bool hasSameSerial = false;
        public bool HasSameSerial
        {
            get => hasSameSerial;
            set => SetProperty(ref hasSameSerial, value);
        }
        public bool IsDouble { get; private set; } = false;

        private double _entityLength;
        public double EntityLength
        {
            get => _entityLength;
            set
            {
                if (Math.Abs(_entityLength - value) > 0.001)
                {
                    _entityLength = value;
                    UpdateModelLength(value);
                    OnPropertyChanged();
                }
            }
        }
        private string _serialCode;
        public string SerialCode
        {
            get => _serialCode;
            set
            {
                if (_serialCode != value)
                {
                    _serialCode = value;
                    UpdateSerialCodeToModel(value);
                    OnPropertyChanged();
                }
            }
        }
        public string TagName => $"{LocationCode}{LevelCode}-{TypeCode}-{SerialCode}";
        public string EntityName { get; private set; }
        public string EntityLevelName { get; private set; }
        public string InstallCode { get; private set; }
        public string TypeCode { get; private set; }
        public string LevelCode { get; private set; }
        public string LocationCode { get; private set; }
        public GuidanceSignEntity(IndependentTag tag)
        {
            if (tag == null) throw new ArgumentNullException(nameof(tag));
            _document = tag.Document ?? throw new ArgumentException("Tag document is null");

            Id = tag.Id;
            EntityId = tag.TaggedLocalElementId;
            Entity = _document.GetElement(EntityId) as FamilyInstance;
            EntityName = Entity.Name;
            EntityLevelName = _document.GetElement(Entity.LevelId).Name;
            LocationCode = Entity.Symbol.LookupParameter("位置编码").AsString();
            LevelCode = Entity.LookupParameter("层高编码").AsString();
            TypeCode = Entity.Symbol.LookupParameter("性质编码").AsString();
            SerialCode = Entity.LookupParameter("本层编号").AsString();
            InstallCode = Entity.Symbol.LookupParameter("悬挂方式").AsString();
            IsDouble = InstallCode != "附着式";

            _entityLength = Math.Round(Entity.LookupParameter("推荐长度").AsDouble() * 304.8, 0);
            EntityContent = Entity.LookupParameter("标识内容").AsString();
            if (EntityContent == "")
            {
                var front = new[]
                {
                                Entity.LookupParameter("文字转换")?.AsString() ?? "",
                                Entity.LookupParameter("文字转换 第二行")?.AsString() ?? "",
                                Entity.LookupParameter("文字转换 第三行")?.AsString() ?? ""
                            };
                string frontLine = string.Join("；", front.Where(s => !string.IsNullOrEmpty(s)));
                // 读取背面三行并用分号拼接
                var backParam = Entity.LookupParameter("文字转换 背面")?.AsString();
                bool hasBackSide = !string.IsNullOrWhiteSpace(backParam);
                string backLine = "";
                if (hasBackSide)
                {
                    var back = new[]
                    {
                                    backParam ?? "",
                                    Entity.LookupParameter("文字转换 第二行背面")?.AsString() ?? "",
                                    Entity.LookupParameter("文字转换 第三行背面")?.AsString() ?? ""
                                };
                    backLine = string.Join("；", back.Where(s => !string.IsNullOrEmpty(s)));
                }
                EntityContent = hasBackSide ? $"{frontLine} |背面：{backLine}" : frontLine;
            }
        }
        private string entityContent;
        public string EntityContent
        {
            get => entityContent;
            set => SetProperty(ref entityContent, value);
        }
        public void UpdateSerialCodeToModel(string newSerialCode)
        {
            _externalHandler.Run(app =>
            {
                using (var tx = new Transaction(_document, "更新编号"))
                {
                    tx.Start();
                    Entity.LookupParameter("本层编号")?.Set(newSerialCode);
                    OnPropertyChanged(nameof(TagName));
                    CheckForDuplicates();
                    tx.Commit();
                }
            });
            SerialCode = newSerialCode; // 同步内存数据
        }
        public void UpdateModelLength(double lengthMm)
        {
            _externalHandler.Run(app =>
            {
                using (var tx = new Transaction(_document, "更新标识长度"))
                {
                    tx.Start();
                    Entity.LookupParameter("推荐长度")?.Set(EntityLength / 304.8);
                    tx.Commit();
                }
            });

            EntityLength = lengthMm; // 同步内存数据
        }
        private void CheckForDuplicates()
        {
            var collector = new FilteredElementCollector(_document);
            var categoryId = this.Entity.Category.Id;
            var otherTags = collector.OfClass(typeof(IndependentTag)).Cast<IndependentTag>().Where(t => t.Id != this.Id);
            bool foundDuplicate = false;
            foreach (var otherTag in otherTags)
            {
                var otherTaggedEntity = this._document.GetElement(otherTag.TaggedLocalElementId) as FamilyInstance;
                if (otherTaggedEntity == null || otherTaggedEntity.Category.Id != categoryId)
                {
                    continue;
                }
                string otherLocationCode = otherTaggedEntity.Symbol.LookupParameter("位置编码")?.AsString() ?? "";
                string otherLevelCode = otherTaggedEntity.LookupParameter("层高编码")?.AsString() ?? "";
                string otherTypeCode = otherTaggedEntity.Symbol.LookupParameter("性质编码")?.AsString() ?? "";
                string otherSerialCode = otherTaggedEntity.LookupParameter("本层编号")?.AsString() ?? "";
                string otherTagName = otherLocationCode + otherLevelCode + "-" + otherTypeCode + "-" + otherSerialCode;
                if (otherTagName == this.TagName)
                {
                    foundDuplicate = true;
                    break; // 找到一个就足够了，可以立即退出循环
                }
            }
            this.HasSameSerial = foundDuplicate;
        }

    }
}
