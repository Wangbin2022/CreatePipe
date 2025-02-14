using Autodesk.Revit.DB;
using RevitPro.PipeSystemManager.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RevitPro.PipeSystemManager.Form
{
    /// <summary>
    /// 新增管道系统窗体
    /// </summary>
    public partial class AddPipeSystemForm : Window
    {
        PipeSystemListForm pipeSystemListForm = null;
        public AddPipeSystemForm(/*List<LinePatternElement> linePatternElements, List<int> lineWeights, List<PipeSystemTypeEntity>pipeSystemTypeEntitys, */PipeSystemListForm pipeSystemListForm)
        {
            InitializeComponent();
            //绑定线型数据
            cb1.ItemsSource = pipeSystemListForm.LinePatternElementInfos; /*linePatternElements;*/
            cb1.SelectedIndex = 0;
            //绑定线宽数据
            cb2.ItemsSource = pipeSystemListForm.LineWeights;/* lineWeights;*/
            cb2.SelectedIndex = 0;

            //系统分类
            systemType_cb.ItemsSource = pipeSystemListForm.PipeSystemTypeEntitys;// pipeSystemTypeEntitys;
            systemType_cb.SelectedIndex = 0;

          
            this.pipeSystemListForm = pipeSystemListForm;
      
        }

        //颜色点击打开
        private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Forms.ColorDialog colorDialog = new System.Windows.Forms.ColorDialog();
            //允许使用该对话框的自定义颜色
            colorDialog.AllowFullOpen = true;
            colorDialog.FullOpen = true;
            colorDialog.ShowHelp = true;
            //初始化当前文本框中的字体颜色，
            colorDialog.Color = System.Drawing.Color.Black;
            //当用户在ColorDialog对话框中点击"取消"按钮
            System.Windows.Forms.DialogResult dialogResult = colorDialog.ShowDialog();
            if (dialogResult != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }
            System.Drawing.Color color = colorDialog.Color;

            TextBlock textBlock = sender as TextBlock;
            textBlock.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(color.R, color.G, color.B));
        }

        //关闭窗体
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public PipeSystemEntity pipeSystemEntity = null;
        //新增管道系统
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            #region 数据检验
            StringBuilder stringBuilder = new StringBuilder();
            //系统名检测
            if (string.IsNullOrEmpty(systemName_tb.Text))
            {

                stringBuilder.AppendLine("系统名不能为空，请输入");
            }

            if (stringBuilder.Length>0)
            {
                MessageBox.Show(stringBuilder.ToString(),"提示");
                return;
            }

            foreach (PipeSystemEntity item in pipeSystemListForm.pipeSystemEntitys)
            {
                if (item.SystemName.Equals(systemName_tb.Text))
                {
                    MessageBox.Show("你输入的系统名已经存在，请更改其它名称", "提示");
                    return;
                } 
            }


            #endregion
            pipeSystemEntity = new PipeSystemEntity() ;
            pipeSystemEntity.SystemName = systemName_tb.Text;
            pipeSystemEntity.Abbreviation = abbreviation_tb.Text;
            pipeSystemEntity.LineWeight = Convert.ToInt32(cb2.SelectedItem);
            pipeSystemEntity.LinePatternElement = cb2.SelectedItem as LinePatternElement;
            pipeSystemEntity.SolidColorBrush = curveColor_tb.Background as SolidColorBrush;
            pipeSystemEntity.PipeSystemTypeEntity = systemType_cb.SelectedItem as PipeSystemTypeEntity;


            this.Close();
        }
    }
}
