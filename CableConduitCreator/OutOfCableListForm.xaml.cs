using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Windows;
using System.Windows.Controls;

namespace CreatePipe.CableConduitCreator
{
    /// <summary>
    /// OutOfCableListForm.xaml 的交互逻辑
    /// </summary>
    public partial class OutOfCableListForm : Window
    {
        //为了实现交乎还要在这里改为外部事件
        ExternalEvent externalEvent;
        ShowElem showElem;
        ElementId showId;
        OutOfCableListVM outOfVM;
        public OutOfCableListForm(OutOfCableListVM outList)
        {
            InitializeComponent();
            this.outOfVM = outList;
            DataContext = outOfVM.Ids;
            showElem = new ShowElem();
            externalEvent = ExternalEvent.Create(showElem);
        }
        private void showLocation_Click(object sender, RoutedEventArgs e)
        {
            if (showElem.OutOfCableElemId != null)
            {
                externalEvent.Raise();
            }
        }
        private void ls_outList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            showId = ((dynamic)ls_outList.SelectedItem).Key as ElementId;
            showElem.OutOfCableElemId = showId;
        }
        private void delete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                outOfVM.Ids.Remove(showId);
            }
            catch
            {
            }
        }
    }
}
