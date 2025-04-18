﻿using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace CreatePipe.Form.UserPageControlTest1
{
    /// <summary>
    /// UserControl1.xaml 的交互逻辑
    /// </summary>
    public partial class UserControl1 : UserControl
    {
        public static event EventHandler<PropertyChangedEventArgs> StaticPropertyChanged;//静态事件处理属性更改
        public static int i = 0;

        private static string _myStr = "0";
        public static string myStr //Label 标签内容绑定到这个myStr字符串
        {
            get { return _myStr; }
            set
            {
                _myStr = value;
                StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(nameof(myStr)));//异步更新属性
            }
        }
        public UserControl1()
        {
            InitializeComponent();
            DataContext = this;//设置绑定数据的上下文为UserControl1类
        }

        //累加按键处理事件
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            i++;//按一下数值累加一次
            myStr = i.ToString();//把累加完后的数值转换成字符串赋给标签内容值（立刻更新标签内容）
        }

        //清空按键处理事件
        private void btn_Clear_Click(object sender, RoutedEventArgs e)
        {
            UserControl1.i = 0;//把累加计数值清空设为0
            UserControl1.myStr = "0";//立刻更新标签内容

            UserControl2.i = 0;//同时把用户界面2里的累加值清空
            UserControl2.myStr = "0";//立刻更新用户界面2的标签内容
        }
    }
}
