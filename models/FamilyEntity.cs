using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace CreatePipe.models
{
    public class FamilyEntity
    {
        public FamilyEntity(Family family)
        {
            Family = family;
            Name = family.Name;
            // 防御性编程：有些内建族可能没有 Category
            Category = family.FamilyCategory?.Name ?? "无类别";
            var symbolIds = family.GetFamilySymbolIds();
            SymbolCount = symbolIds.Count;
            // 使用字典存储，Key是名称，Value是Symbol对象本身
            SymbolDict = new Dictionary<string, FamilySymbol>();
            foreach (ElementId id in symbolIds)
            {
                if (family.Document.GetElement(id) is FamilySymbol symbol)
                {
                    SymbolDict[symbol.Name] = symbol;
                }
            }
        }
        public Family Family { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public int SymbolCount { get; set; }
        // 核心修改：改为字典
        public Dictionary<string, FamilySymbol> SymbolDict { get; set; }
    }
    //public class FamilyEntity
    //{
    //    public FamilyEntity(Family family)
    //    {
    //        Family = family;
    //        Name = family.Name;
    //        Category = family.FamilyCategory.Name;
    //        SymbolCount = family.GetFamilySymbolIds().Count();
    //        foreach (var item in family.GetFamilySymbolIds())
    //        {
    //            FamilySymbol familySymbol = (FamilySymbol)family.Document.GetElement(item);
    //            FamilySymbols.Add(familySymbol.Name);
    //        }
    //    }
    //    public Family Family { get; set; }
    //    public string Name { get; set; }
    //    public string Category { get; set; }
    //    public int SymbolCount { get; set; }
    //    public List<string> FamilySymbols { get; set; } = new List<string>();
    //}
}
