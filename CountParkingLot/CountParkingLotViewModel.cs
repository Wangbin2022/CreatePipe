using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CreatePipe.cmd;
using CreatePipe.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace CreatePipe.CountParkingLot
{
    public class CountParkingLotViewModel : ObserverableObject
    {
        public Document Doc { get; set; }
        public CountParkingLotViewModel(UIApplication uiApp, IList<ElementId> Ids)
        {
            Doc = uiApp.ActiveUIDocument.Document;
            ParkingLotNum = Ids.Count();
            ParkReference = Ids;
            Items = new ObservableCollection<ComboBoxItem>
        {
            new ComboBoxItem { DisplayText = "默认格式", Value = 1 },
            new ComboBoxItem { DisplayText = "补一个0", Value = 2 },
            new ComboBoxItem { DisplayText = "补两个0", Value = 3 },
            new ComboBoxItem { DisplayText = "补三个0", Value = 4 }
        };
            // 设置默认选中第一项
            if (Items.Count > 0)
            {
                SelectedValue = Items[0].Value;
            }
        }
        private IList<ElementId> ParkReference;
        public ICommand CodeAllCommand => new BaseBindingCommand(RewriteCode);
        private void RewriteCode(object obj)
        {
            XmlDoc.Instance.Task.Run(app =>
            {
                Doc.NewTransaction(() =>
                {
                    try
                    {
                        int startCodeValue = startCode;
                        int endCodeValue = startCodeValue + ParkingLotNum - 1;
                        for (int i = 0; i < ParkingLotNum; i++)
                        {
                            Element element = Doc.GetElement(ParkReference[i]);
                            if (element is FamilyInstance familyInstance)
                            {
                                string lotSn;
                                switch (SelectedValue)
                                {
                                    case 2:
                                        // 如果 startCodeValue 到 endCodeValue 出现 1-9 数值，前面补 0
                                        lotSn = $"{Prefix}{startCodeValue + i:D2}";
                                        familyInstance.LookupParameter("车位编号").Set(lotSn);
                                        break;
                                    case 3:
                                        lotSn = $"{Prefix}{startCodeValue + i:D3}";
                                        familyInstance.LookupParameter("车位编号").Set(lotSn);
                                        break;
                                    case 4:
                                        lotSn = $"{Prefix}{startCodeValue + i:D4}";
                                        familyInstance.LookupParameter("车位编号").Set(lotSn);
                                        break;
                                    default:
                                        //直接以所选集合顺序给号
                                        lotSn = $"{Prefix}{startCodeValue + i}";
                                        familyInstance.LookupParameter("车位编号").Set(lotSn);
                                        break;
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }, "写入车位编号");
            });
        }
        private int _selectedValue;
        public int SelectedValue
        {
            get => _selectedValue;
            set
            {
                _selectedValue = value;
                OnPropertyChanged(nameof(SelectedValue));
                UpdateCodePreview();
            }
        }
        public ObservableCollection<ComboBoxItem> Items { get; set; }
        private void UpdateCodePreview()
        {
            int startCodeValue = startCode;
            int endCodeValue = startCodeValue + ParkingLotNum - 1;
            switch (SelectedValue)
            {
                case 2:
                    // 如果 startCodeValue 到 endCodeValue 出现 1-9 数值，前面补 0
                    CodePreview = $"{Prefix}{startCodeValue:D2} - {Prefix}{endCodeValue:D2}";
                    break;
                case 3:
                    CodePreview = $"{Prefix}{startCodeValue:D3} - {Prefix}{endCodeValue:D3}";
                    break;
                case 4:
                    CodePreview = $"{Prefix}{startCodeValue:D4} - {Prefix}{endCodeValue:D4}";
                    break;
                default:
                    CodePreview = $"{Prefix}{startCodeValue} - {Prefix}{endCodeValue}";
                    break;
            }
        }
        private string codePreview;
        public string CodePreview
        {
            get { return codePreview; }
            set
            {
                codePreview = value;
                OnPropertyChanged();
            }
        }
        private int startCode;
        public int StartCode
        {
            get { return startCode; }
            set
            {
                startCode = value;
                OnPropertyChanged();
                UpdateCodePreview();
            }
        }
        private string prefix;
        public string Prefix
        {
            get => prefix;
            set
            {
                prefix = value;
                OnPropertyChanged();
                UpdateCodePreview();
            }
        }
        public int ParkingLotNum { get; set; }
    }
    public class ComboBoxItem
    {
        public string DisplayText { get; set; }
        public int Value { get; set; }
    }
}
