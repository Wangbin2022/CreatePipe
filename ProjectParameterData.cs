using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe
{
    public class ProjectParameterData
    {
        public string Name { get; set; }
        public string ParameterType { get; set; }
        public string ParameterGroup { get; set; }
        public string BindingType { get; set; }
        public List<string> Categories { get; set; }
        public bool IsShared { get; set; }
        public bool IsReportable { get; set; }
        public string GUID { get; set; }
    }
}
