using System.Windows;

namespace CreatePipe.Form
{

    /// <summary>
    /// BreakMEPCurveForm.xaml 的交互逻辑
    /// </summary>
    public partial class BreakMEPCurveForm : Window
    {
        //BreakMEPCurveWPF breakMEPCurveCmd = null;
        ////ExternalEvent externalEvent = null;
        //ExternalEventExample externalEventExample = null;
        ////使用事件将ExternalEvent externalEvent 替换为 ExternalEventExample externalEventExample
        //public BreakMEPCurveForm(BreakMEPCurveWPF breakMEPCurveCmd, ExternalEventExample externalEventExample)
        //{
        //    this.breakMEPCurveCmd = breakMEPCurveCmd;
        //    this.externalEventExample = externalEventExample;
        //    InitializeComponent();
        //    this.Topmost = true;//强制窗口置顶
        //}

        private void RadioButton_Click_1(object sender, RoutedEventArgs e)
        {
            ////使用委托前事件OK
            ////ExternalEventExample.id = 01;
            ////externalEvent.Raise();

            ////使用委托后
            //externalEventExample.ClearExternal();
            ////externalEventExample.External += breakMEPCurveCmd.BreakMEPCurveByOneV;
            //externalEventExample.Implement();
        }

        private void RadioButton_Click_2(object sender, RoutedEventArgs e)
        {
            ////使用委托前事件OK
            ////ExternalEventExample.id = 1;
            ////externalEvent.Raise();

            ////使用委托后
            //externalEventExample.ClearExternal();
            ////externalEventExample.External += breakMEPCurveCmd.BreakMEPCurveByTwoV;
            //externalEventExample.Implement();
        }
    }
}
