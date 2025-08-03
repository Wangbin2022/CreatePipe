using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe.models
{
    public class AdaptiveRouteEntity : ObserverableObject
    {
        Document Document {  get; set; }
        public AdaptiveRouteEntity(FamilyInstance familyInstance)
        {
            Document= familyInstance.Document;
            entityName = familyInstance.Symbol.Family.Name;
            Id = familyInstance.Id;
            levelName = familyInstance.LookupParameter("楼层标高").AsString();
            totalLength = (familyInstance.LookupParameter("总长度").AsDouble() * 304.8).ToString("F2");
            //GetIntersectRoom(familyInstance);

            rooms=GetRoomsCrossFamilyInstance(familyInstance).Count();
        }
        public List<ElementId> GetRoomsCrossFamilyInstance(FamilyInstance familyInstance)
        {
            List<ElementId> roomIds = new List<ElementId>();

            // 获取族实例的几何体
            Options options = new Options();
            options.ComputeReferences = true;
            options.DetailLevel = ViewDetailLevel.Fine;

            GeometryElement geomElem = familyInstance.get_Geometry(options);
            if (geomElem == null) return roomIds;

            // 收集所有房间
            FilteredElementCollector roomCollector = new FilteredElementCollector(Document)
                .OfCategory(BuiltInCategory.OST_Rooms);

            foreach (Room room in roomCollector)
            {
                // 获取房间的几何体
                GeometryElement roomGeom = room.get_Geometry(options);
                if (roomGeom == null) continue;

                // 检查几何体是否相交
                if (GeometriesIntersect(geomElem, roomGeom))
                {
                    roomIds.Add(room.Id);
                }
            }

            return roomIds;
        }

        private bool GeometriesIntersect(GeometryElement geom1, GeometryElement geom2)
        {
            foreach (GeometryObject obj1 in geom1)
            {
                Solid solid1 = obj1 as Solid;
                if (solid1 == null || solid1.Faces.Size == 0) continue;

                foreach (GeometryObject obj2 in geom2)
                {
                    Solid solid2 = obj2 as Solid;
                    if (solid2 == null || solid2.Faces.Size == 0) continue;

                    // 检查两个实体是否相交
                    try
                    {
                        Solid intersection = BooleanOperationsUtils.ExecuteBooleanOperation(solid1, solid2, BooleanOperationsType.Intersect);
                        if (intersection != null && intersection.Volume > 0)
                        {
                            return true;
                        }
                    }
                    catch
                    {
                        // 忽略布尔运算错误
                    }
                }
            }

            return false;
        }

        public int rooms {  get; set; }
        //找出相关房间、门，验证内部长度
        private void GetIntersectRoom(FamilyInstance familyInstance)
        {
            //Element element = doc.GetElement(elementId);
            //FilteredElementCollector collect = new FilteredElementCollector(doc);
            ////冲突检查
            //ElementIntersectsElementFilter iFilter = new ElementIntersectsElementFilter(element, false);
            //collect.WherePasses(iFilter);
        }
        public string totalLength { get; set; }
        public string entityName { get; set; }
        public string levelName { get; set; }
        public ElementId Id { get; set; }
    }
}
