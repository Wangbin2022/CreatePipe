﻿using CreatePipe.Form.UserPageControlTest1;
using System;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Media;

namespace CreatePipe.Form.UserPageControlTest2
{
    public class MainWindowVeiwModel : INotifyPropertyChanged
    {
        private UserControl _content;
        private SampleVm[] _Items;
        public int SelectId;

        public MainWindowVeiwModel()//在构造方法中向Items注册各个用户控件、id以及对应的标题
        {
            _Items = new[]
            {
                new SampleVm("页面1", typeof(UserControl1),0),
                new SampleVm("页面2", typeof(UserControl2),1),
            };
        }
        //ItemsControl中的数据模板绑定SampleVm类数组
        public SampleVm[] Items
        {
            get { return _Items; }
            set
            {
                _Items = value;
                OnPropertyChanged("Items");
            }
        }
        //主页面的内容呈现器
        public UserControl Content
        {
            get { return _content; }
            set
            {
                _content = value;
                OnPropertyChanged("Content");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    public class SampleVm : INotifyPropertyChanged
    {
        private SolidColorBrush _BackColor;
        public SampleVm(string title, Type content, int id)
        {
            Title = title;
            Content = content;
            Id = id;
        }
        public int Id { get; private set; }
        public string Title { get; set; }
        public Type Content { get; set; }

        public SolidColorBrush BackColor
        {
            get { return _BackColor; }
            set { _BackColor = value; OnPropertyChanged("BackColor"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
