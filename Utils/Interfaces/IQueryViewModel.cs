using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CreatePipe.Utils.Interfaces
{
    internal interface IQueryViewModel<T>
    {
        // 1. 初始化方法 (隐式为 public)
        void InitLayers();
        // 2. 查询命令属性
        ICommand QueryElementCommand { get; }
        void QueryELement(string text);
        // 3. 动态数据集合属性，用于界面绑定列表
        ObservableCollection<T> Collection { get; set; }
    }
}
