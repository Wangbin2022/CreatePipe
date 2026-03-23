using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;

namespace CreatePipe.Utils
{
    public static class JsonHelper
    {
        // 配置 Newtonsoft.Json 序列化选项
        private static readonly JsonSerializerSettings _settings = new JsonSerializerSettings
        {
            // 启用自动缩进，使生成的 JSON 文件清晰可读（换行+缩进）
            Formatting = Formatting.Indented,
            // 忽略空值（如果某个属性为 null，则不写入 JSON 文件，让文件更精简）
            NullValueHandling = NullValueHandling.Ignore
        };

        /// <summary>
        /// 将对象序列化并保存到 JSON 文件
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="filePath">保存路径</param>
        /// <param name="data">要保存的数据对象</param>
        public static void SaveToFile<T>(string filePath, T data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            // 1. 序列化为 JSON 字符串
            string jsonString = JsonConvert.SerializeObject(data, _settings);
            // 2. 统一使用带 BOM 的 UTF-8 写入（防止中文路径或中文内容出现乱码）
            File.WriteAllText(filePath, jsonString, new UTF8Encoding(true));
        }

        /// <summary>
        /// 从 JSON 文件读取并反序列化为对象
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="filePath">文件路径</param>
        /// <returns>反序列化后的对象，如果文件不存在或为空则返回 default(T)</returns>
        public static T LoadFromFile<T>(string filePath)
        {
            if (!File.Exists(filePath)) return default;
            // 1. 以 UTF-8 编码读取文件内容
            string jsonString = File.ReadAllText(filePath, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(jsonString)) return default;
            // 2. 反序列化为指定类型的对象
            // Newtonsoft.Json 默认支持属性名的大小写不敏感匹配
            return JsonConvert.DeserializeObject<T>(jsonString, _settings);
        }
    }
    ///// <summary>
    ///// 基于 System.Text.Json 的 Json 读写辅助类 NET8以后可用，其它版本Revit自带Newtonsoft.Json
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
