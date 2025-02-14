using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using CreatePipe.PipeSystemManager.Entity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

using System.Windows.Input;
using System.Windows.Media;

namespace CreatePipe.PipeSystemManager.Form
{
    /// <summary>
    /// 管道系统列表
    /// </summary>
    public partial class PipeSystemListForm : Window
    {
        //数据源
        public ObservableCollection<PipeSystemEntity> pipeSystemEntitys = null;
        //线型
        List<LinePatternElement> linePatternElementInfos = null;
        List<MEPSystemTypeEntity> pipeSystemTypeEntities = null;
        Document document = null;
        TransactionGroup transactionGroup = null;
        public PipeSystemListForm(ObservableCollection<PipeSystemEntity> pipeSystemEntitys, Document document, List<LinePatternElement> linePatternElementInfos)
        {
            this.linePatternElementInfos = linePatternElementInfos;
            this.document = document;
            InitializeComponent();

            //管道系统，从cmd传值后处理，绑定到datagrid combobox
            this.pipeSystemEntitys = pipeSystemEntitys;
            dataGrid.ItemsSource = this.pipeSystemEntitys;
            List<MEPSystemTypeEntity> pipeSystemTypeEntities = new List<MEPSystemTypeEntity>() {
            new MEPSystemTypeEntity() {Name="其他",MEPSystemClassification= MEPSystemClassification.OtherPipe},
                new MEPSystemTypeEntity() { Name = "其他消防系统", MEPSystemClassification = MEPSystemClassification.FireProtectOther },
                  new MEPSystemTypeEntity() { Name = "卫生设备", MEPSystemClassification = MEPSystemClassification.Sanitary },
                    new MEPSystemTypeEntity() { Name = "家用冷水", MEPSystemClassification = MEPSystemClassification.DomesticColdWater },
                      new MEPSystemTypeEntity() { Name = "家用热水", MEPSystemClassification = MEPSystemClassification.DomesticHotWater },
                        new MEPSystemTypeEntity() { Name = "干式消防系统", MEPSystemClassification = MEPSystemClassification.FireProtectDry },
                          new MEPSystemTypeEntity() { Name = "循环供水", MEPSystemClassification = MEPSystemClassification.SupplyHydronic },
                            new MEPSystemTypeEntity() { Name = "循环回水", MEPSystemClassification = MEPSystemClassification.ReturnHydronic },
                              new MEPSystemTypeEntity() { Name = "湿式消防系统", MEPSystemClassification = MEPSystemClassification.FireProtectWet },
                                new MEPSystemTypeEntity() { Name = "通风孔", MEPSystemClassification = MEPSystemClassification.Vent },
                                  new MEPSystemTypeEntity() { Name = "预作用消防系统", MEPSystemClassification = MEPSystemClassification.FireProtectPreaction }
                                   };

            transactionGroup = new TransactionGroup(document, "group");
            transactionGroup.Start();
        }
        /// <summary>
        /// 线宽集合
        /// </summary>
        public List<int> LineWeights
        {
            get
            {
                List<int> ints = new List<int>();
                for (int i = 1; i <= 16; i++)
                {
                    ints.Add(i);
                }
                return ints;
            }
            set { LineWeights = value; }
        }
        /// <summary>
        /// 线型
        /// </summary>
        public List<LinePatternElement> LinePatternElementInfos
        {
            get
            {
                return linePatternElementInfos;
            }
            set { LinePatternElementInfos = value; }
        }
        public List<MEPSystemTypeEntity> PipeSystemTypeEntitys
        {
            get
            {
                return pipeSystemTypeEntities;
            }
            set
            {
                PipeSystemTypeEntitys = value;
            }
        }
        //打开颜色窗体
        private void line_color_tb_MouseDown(object sender, MouseButtonEventArgs e)
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
            PipeSystemEntity pipeSystemEntity = dataGrid.SelectedItem as PipeSystemEntity;
            if (pipeSystemEntity != null)
            {
                pipeSystemEntity.IsUpdate = true;
            }
        }
        //保存
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //重名检测
            using (Transaction tran = new Transaction(document, "tran"))
            {
                tran.Start();
                foreach (PipeSystemEntity entity in pipeSystemEntitys)
                {
                    if (entity.IsUpdate)
                    {
                        PipingSystemType pipingSystemType = entity.PipingSystemType;
                        if (!string.IsNullOrEmpty(entity.SystemName))
                        {
                            pipingSystemType.Name = entity.SystemName;
                        }
                        if (!string.IsNullOrEmpty(entity.Abbreviation))
                        {
                            pipingSystemType.Abbreviation = entity.Abbreviation;
                        }
                        if (entity.LinePatternElement != null)
                        {
                            pipingSystemType.LinePatternId = entity.LinePatternElement.Id;
                        }
                        if (entity.LineWeight != 0)
                        {
                            pipingSystemType.LineWeight = entity.LineWeight;
                        }
                        if (entity.SolidColorBrush != null)
                        {
                            System.Windows.Media.Color color = entity.SolidColorBrush.Color;
                            pipingSystemType.LineColor = new Autodesk.Revit.DB.Color(color.R, color.G, color.B);
                        }
                    }
                }
                tran.Commit();
            }
            transactionGroup.Assimilate();
            this.Close();
        }
        //系统名称更改
        private void systemTypeName_tb_TextChanged(object sender, TextChangedEventArgs e)
        {
            PipeSystemEntity pipeSystemEntity = dataGrid.SelectedItem as PipeSystemEntity;
            if (pipeSystemEntity != null)
            {
                pipeSystemEntity.IsUpdate = true;
            }
        }
        //线宽和线型改变事件
        private void cb_Type_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PipeSystemEntity pipeSystemEntity = dataGrid.SelectedItem as PipeSystemEntity;
            if (pipeSystemEntity != null)
            {
                pipeSystemEntity.IsUpdate = true;
            }
        }
        //删除功能
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            //获取窗体选中的系统集合
            List<PipeSystemEntity> pipeSystemEntities = dataGrid.SelectedItems.Cast<PipeSystemEntity>().ToList();
            //看数量是否大于0
            if (pipeSystemEntities.Count > 0)
            {
                //询问是否删除
                MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("是否删除?", "提示", MessageBoxButton.OKCancel);
                //ok删除
                if (messageBoxResult == MessageBoxResult.OK)
                {
                    StringBuilder stringBuilder = new StringBuilder();//记录异常
                    //删除revit中实际的系统
                    //删除集合中的数据
                    using (Transaction tran = new Transaction(document, "delete"))
                    {
                        tran.Start();
                        foreach (PipeSystemEntity system in pipeSystemEntities)
                        {
                            try
                            {
                                document.Delete(system.PipingSystemType.Id);
                                pipeSystemEntitys.Remove(system);
                            }
                            catch (Exception)//这里可以输出错误到详细描述
                            {
                                stringBuilder.AppendLine(system.PipingSystemType.Name);
                            }
                        }
                        tran.Commit();
                    }
                    if (stringBuilder.Length > 0)
                    {
                        stringBuilder.AppendLine("无法删除!");
                        MessageBox.Show(stringBuilder.ToString());
                    }
                }
            }
        }
        //新增系统
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            AddPipeSystemForm addPipeSystemForm = new AddPipeSystemForm(this);
            addPipeSystemForm.ShowDialog();

            if (addPipeSystemForm.pipeSystemEntity != null)
            {
                PipeSystemEntity pipeSystemEntity = addPipeSystemForm.pipeSystemEntity;
                //revit中添加数据
                using (Transaction tran = new Transaction(document, "addSystem"))
                {
                    tran.Start();
                    pipeSystemEntity.PipingSystemType = PipingSystemType.Create(document, MEPSystemClassification.SupplyHydronic, pipeSystemEntity.SystemName);
                    tran.Commit();
                }
                //集合添加一条
                pipeSystemEntitys.Add(pipeSystemEntity);
            }
        }
        //取消操作
        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            transactionGroup.RollBack();
            this.Close();
        }
    }
}
