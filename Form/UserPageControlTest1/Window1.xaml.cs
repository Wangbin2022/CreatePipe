﻿using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace CreatePipe.Form.UserPageControlTest1
{
    /// <summary>
    /// Window1.xaml 的交互逻辑
    /// </summary>
    public partial class Window1 : Window, INotifyPropertyChanged
    {
        //简易页面切换实现
        private UserControl1 UserControl1 = new UserControl1();//实例化用户控件1
        private UserControl2 UserControl2 = new UserControl2();//实例化用户控件2
        public Window1()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void ButtonClick1(object sender, RoutedEventArgs e)
        {
            UserContent = UserControl1;//内容呈现器绑定的UserContent赋值给用户控件1
        }

        private void ButtonClick2(object sender, RoutedEventArgs e)
        {
            UserContent = UserControl2;//内容呈现器绑定的UserContent赋值给用户控件2
        }
        private UserControl _content;
        //内容呈现器绑定到UserContent
        public UserControl UserContent
        {
            get { return _content; }
            set
            {
                _content = value;
                OnPropertyChanged("UserContent");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
