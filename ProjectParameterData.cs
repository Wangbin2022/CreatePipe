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
        //*PARAM	GUID	NAME	DATATYPE	DATACATEGORY	GROUP	VISIBLE	DESCRIPTION	USERMODIFIABLE	HIDEWHENNOVALUE
        //PARAM	43e57303-6590-404e-8d91-917b70bb5109 无障碍 TEXT		1	1		1	0
    }
}
