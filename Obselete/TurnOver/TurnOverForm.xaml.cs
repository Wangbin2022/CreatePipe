using CreatePipe.utils;
using System.Windows;
using System.Windows.Input;

namespace CreatePipe.TurnOver
{
    /// <summary>
    /// TurnOverForm.xaml 的交互逻辑
    /// </summary>
    public partial class TurnOverForm : Window
    {
        //MEPCurveTurnOverCmd mEPCurveTurnOverCmd = null;
        //ExternalEventExample externalEventExample = null;
        //public TurnOverForm(MEPCurveTurnOverCmd mEPCurveTurnOverCmd, ExternalEventExample externalEventExample)
        //{
        //    this.externalEventExample = externalEventExample;
        //    this.mEPCurveTurnOverCmd = mEPCurveTurnOverCmd;
        //    InitializeComponent();
        //    this.Topmost = true;
        //    TurnOverEntity turnOverEntity = XMLUtil.DeserializeFromXml<TurnOverEntity>(@"D:\newXml.xml");

        //    if (turnOverEntity != null)
        //    {
        //        turnOverHeight_tb.Text = turnOverEntity.Height;
        //        angle_tb.Text = turnOverEntity.Angle;
        //    }
        //    this.KeyDown += Exit_KeyDown;
        //}


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //    externalEventExample.ClearExternal();
            //    //externalEventExample.External += mEPCurveTurnOverCmd.TurnOver;
            //    externalEventExample.Implement();
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            string height = turnOverHeight_tb.Text;
            string angle = angle_tb.Text;
            TurnOverEntity turnOver = new TurnOverEntity();
            turnOver.Height = height;
            turnOver.Angle = angle;
            XMLUtil.SerializeToXml(@"D:\newXml.xml", turnOver);
            MessageBox.Show("已保存设置，可关闭窗口");
        }

        private void Exit_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)//Esc键  
            {
                this.Close();
            }
        }

        private void Button_Click2(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
