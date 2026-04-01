using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CreatePipe.Utils.Interfaces
{
    public interface IQueryViewModelWithDelete<T>
    {
        // 1. 初始化与数据加载
        void InitFunc();

        // 2. 核心数据集合
        ObservableCollection<T> Collection { get; set; }

        // 3. 基础逻辑命令
        ICommand QueryElementCommand { get; }
        ICommand DeleteElementCommand { get; }      // 单个删除
        ICommand DeleteElementsCommand { get; }     // 批量删除

        // 4. 通用外部事件处理器 (封装 Revit 线程操作)
        BaseExternalHandler ExternalHandler { get; }

        // 5. 逻辑实现方法
        void QueryElement(string text);
        void DeleteElement(T entity);
        void DeleteElements(IEnumerable<object> selectedItems);
    }
}
