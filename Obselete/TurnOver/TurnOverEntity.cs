using System.Xml.Serialization;

namespace CreatePipe.TurnOver
{
    [XmlType(TypeName = "TurnOverEntity")]
    public class TurnOverEntity
    {
        [XmlAttribute]
        public string Height { get; set; }
        [XmlAttribute]
        public string Angle { get; set; }
    }
}
