using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace CreatePipe.ViewFilters
{
    /// <summary>
    /// NewFilterForm.xaml 的交互逻辑
    /// </summary>
    public partial class NewFilterForm : Window
    {
        ICollection<String> m_inUseFilterNames;
        private String m_filterName;//新过滤器名称        
        public String FilterName
        {
            get { return m_filterName; }
        }
        public NewFilterForm(ICollection<String> inUseNames)
        {
            InitializeComponent();
            m_inUseFilterNames = inUseNames;
        }

        //1124 按键功能要转移到VM中实现，要检验输入值需要使用Icommand和INotify
        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            String newName = newFilterNameTextBox.Text.Trim();
            if (String.IsNullOrEmpty(newName))
            {
                MyMessageBox("过滤器名称不可为空!");
                newFilterNameTextBox.Focus();
                return;
            }
            // Check if filter name contains invalid characters
            // These character are different from Path.GetInvalidFileNameChars()
            char[] invalidFileChars = { '\\', ':', '{', '}', '[', ']', '|', ';', '<', '>', '?', '\'', '~' };
            foreach (char invalidChr in invalidFileChars)
            {
                if (newName.Contains(invalidChr))
                {
                    MyMessageBox("过滤器名称包含非法字符: " + invalidChr);
                    return;
                }
            }
            // 
            // Check if name is used
            // check if name is already used by other filters
            bool inUsed = m_inUseFilterNames.Contains(newName, StringComparer.OrdinalIgnoreCase);
            if (inUsed)
            {
                MyMessageBox("与已有过滤器名称重复. 请重新输入.");
                newFilterNameTextBox.Focus();
                return;
            }
            m_filterName = newName;
            this.DialogResult = true;
            this.Close();
        }
        public static void MyMessageBox(String strMsg)
        {
            Autodesk.Revit.UI.TaskDialog.Show("View Filters", strMsg, Autodesk.Revit.UI.TaskDialogCommonButtons.Ok | Autodesk.Revit.UI.TaskDialogCommonButtons.Cancel);
        }
    }
}
