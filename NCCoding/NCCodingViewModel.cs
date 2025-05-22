using Autodesk.Revit.UI;
using CreatePipe.cmd;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreatePipe.NCCoding
{
    public class NCCodingViewModel : ObserverableObject
    {

        public NCCodingViewModel(UIApplication uiApp)
        {
            
        }
        private ObservableCollection<NCCodingEntity> nCCodingObj= new ObservableCollection<NCCodingEntity>();

        public ObservableCollection<NCCodingEntity> NCCodingObj 
        { 
            get => nCCodingObj; 
            set => SetProperty(ref nCCodingObj, value);
        }
    }
}
