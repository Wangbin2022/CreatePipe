namespace CreatePipe.Utils
{
    ///// <summary>
    ///// 基于 System.Text.Json 的 Json 读写辅助类
    ///// </summary>
    //public static class JsonHelperSystem
    //{
    //    // 配置 JSON 序列化选项
    //    private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
    //    {
    //        WriteIndented = true, // 启用自动缩进，使生成的 JSON 文件清晰可读
    //        // 允许中文字符不被 Unicode 转义（防止中文变成 \uXXXX）
    //        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    //        PropertyNameCaseInsensitive = true // 读取时忽略大小写
    //    };

    //    /// <summary>
    //    /// 将对象序列化并保存到 JSON 文件
    //    /// </summary>
    //    public static void SaveToFile<T>(string filePath, T data)
    //    {
    //        if (data == null) throw new ArgumentNullException(nameof(data));

    //        string jsonString = JsonSerializer.Serialize(data, _options);
    //        // 统一使用带 BOM 的 UTF-8 写入
    //        File.WriteAllText(filePath, jsonString, new UTF8Encoding(true));
    //    }

    //    /// <summary>
    //    /// 从 JSON 文件读取并反序列化为对象
    //    /// </summary>
    //    public static T LoadFromFile<T>(string filePath)
    //    {
    //        if (!File.Exists(filePath))
    //            return default;

    //        string jsonString = File.ReadAllText(filePath, Encoding.UTF8);
    //        if (string.IsNullOrWhiteSpace(jsonString))
    //            return default;

    //        return JsonSerializer.Deserialize<T>(jsonString, _options);
    //    }
    //}
}
//2.更新 ViewModel 中的保存和加载逻辑
//将你原来 SlopeCheckViewModel 里的 SaveConfig 和 LoadConfig 方法替换为下面的 JSON 版本，并修改文件后缀过滤：

//csharp
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using Autodesk.Revit.UI;
//using Microsoft.Win32;

//namespace CreatePipe
//{
//    public partial class SlopeCheckViewModel
//    {
//        // ... 保留其他代码 ...

//        /// <summary>
//        /// 将当前搜索条件保存到 JSON 文件
//        /// </summary>
//        private void SaveConfig(object obj)
//        {
//            try
//            {
//                SaveFileDialog saveFileDialog = new SaveFileDialog
//                {
//                    Filter = "JSON 配置文件 (*.json)|*.json",
//                    Title = "保存坡度检查条件",
//                    FileName = "坡度检查配置.json"
//                };

//                if (saveFileDialog.ShowDialog() == true)
//                {
//                    // 1. 组装要保存的配置对象
//                    var config = new SlopeCheckConfig
//                    {
//                        SelectedCategories = SelectedItems?.ToList() ?? new List<string>(),
//                        DefaultSymbol = this.DefaultSymbol ?? "",
//                        DefaultNum = this.DefaultNum ?? "",
//                        IsOptionChecked = this.IsOptionChecked,
//                        OptionSymbol = this.OptionSymbol ?? "",
//                        OptionNum = this.OptionNum ?? ""
//                    };

//                    // 2. 使用 JsonHelper 写入文件
//                    JsonHelper.SaveToFile(saveFileDialog.FileName, config);

//                    TaskDialog.Show("成功", "搜索条件保存成功！\n文件路径：" + saveFileDialog.FileName);
//                }
//            }
//            catch (Exception ex)
//            {
//                TaskDialog.Show("错误", "保存 JSON 失败。\n详情：" + ex.Message);
//            }
//        }

//        /// <summary>
//        /// 从 JSON 文件加载搜索条件
//        /// </summary>
//        private void LoadConfig(object obj)
//        {
//            try
//            {
//                OpenFileDialog openFileDialog = new OpenFileDialog
//                {
//                    Filter = "JSON 配置文件 (*.json)|*.json",
//                    Title = "加载坡度检查条件"
//                };

//                if (openFileDialog.ShowDialog() == true)
//                {
//                    // 1. 使用 JsonHelper 读取并反序列化
//                    var config = JsonHelper.LoadFromFile<SlopeCheckConfig>(openFileDialog.FileName);

//                    if (config != null)
//                    {
//                        // 2. 恢复选中类别
//                        SelectedItems.Clear();
//                        if (config.SelectedCategories != null)
//                        {
//                            foreach (var cat in config.SelectedCategories)
//                            {
//                                SelectedItems.Add(cat);
//                            }
//                        }

//                        // 3. 恢复主条件和附加条件
//                        this.DefaultSymbol = config.DefaultSymbol;
//                        this.DefaultNum = config.DefaultNum;
//                        this.IsOptionChecked = config.IsOptionChecked;
//                        this.OptionSymbol = config.OptionSymbol;
//                        this.OptionNum = config.OptionNum;

//                        TaskDialog.Show("成功", "搜索条件加载成功！");
//                    }
//                    else
//                    {
//                        TaskDialog.Show("提示", "读取到的 JSON 文件为空或格式不正确。");
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                TaskDialog.Show("错误", "加载 JSON 失败，请检查文件格式。\n详情：" + ex.Message);
//            }
//        }
//    }
//}







