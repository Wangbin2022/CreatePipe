using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CreatePipe.Utils
{
    /// <summary>
    /// 简单的CSV读写辅助类（不处理引号和逗号转义）
    /// </summary>
    //public class CsvHelper
    //{
    //    private readonly string _filePath;
    //    private readonly Encoding _encoding;
    //    private readonly string _separator;
    //    /// <summary>
    //    /// 初始化CsvHelper
    //    /// </summary>
    //    /// <param name="filePath">CSV文件路径</param>
    //    /// <param name="encoding">编码格式（默认UTF-8）</param>
    //    /// <param name="separator">分隔符（默认逗号）</param>
    //    public CsvHelper(string filePath, Encoding encoding = null, string separator = ",")
    //    {
    //        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    //        _encoding = encoding ?? Encoding.UTF8;
    //        _separator = separator ?? ",";
    //    }
    //    #region 读取方法
    //    /// <summary>
    //    /// 读取所有行，返回字符串列表（每行是一个字符串数组）
    //    /// </summary>
    //    public List<string[]> ReadAll()
    //    {
    //        var result = new List<string[]>();
    //        if (!File.Exists(_filePath)) return result;
    //        using (var reader = new StreamReader(_filePath, _encoding))
    //        {
    //            string line;
    //            while ((line = reader.ReadLine()) != null)
    //            {
    //                if (string.IsNullOrWhiteSpace(line)) continue;
    //                var fields = line.Split(new[] { _separator }, StringSplitOptions.None);
    //                result.Add(fields);
    //            }
    //        }
    //        return result;
    //    }
    //    /// <summary>
    //    /// 读取所有行，返回List<Dictionary<string, string>>（使用第一行作为标题）
    //    /// </summary>
    //    public List<Dictionary<string, string>> ReadAllWithHeaders()
    //    {
    //        var result = new List<Dictionary<string, string>>();
    //        if (!File.Exists(_filePath)) return result;
    //        var lines = File.ReadAllLines(_filePath, _encoding);
    //        if (lines.Length < 2) return result;
    //        // 第一行是标题
    //        var headers = lines[0].Split(new[] { _separator }, StringSplitOptions.None);
    //        // 从第二行开始读取数据
    //        for (int i = 1; i < lines.Length; i++)
    //        {
    //            if (string.IsNullOrWhiteSpace(lines[i])) continue;
    //            var fields = lines[i].Split(new[] { _separator }, StringSplitOptions.None);
    //            var dict = new Dictionary<string, string>();
    //            for (int j = 0; j < headers.Length && j < fields.Length; j++)
    //            {
    //                dict[headers[j].Trim()] = fields[j];
    //            }
    //            result.Add(dict);
    //        }
    //        return result;
    //    }
    //    /// <summary>
    //    /// 读取指定行
    //    /// </summary>
    //    public string[] ReadLine(int lineNumber)
    //    {
    //        if (lineNumber < 0)
    //            throw new ArgumentOutOfRangeException(nameof(lineNumber), "行号不能为负数");
    //        var lines = File.ReadAllLines(_filePath, _encoding);
    //        if (lineNumber >= lines.Length) return null;
    //        return lines[lineNumber].Split(new[] { _separator }, StringSplitOptions.None);
    //    }
    //    /// <summary>
    //    /// 读取指定范围的行的字段值
    //    /// </summary>
    //    public List<string[]> ReadRange(int startLine, int count)
    //    {
    //        var result = new List<string[]>();
    //        var lines = File.ReadAllLines(_filePath, _encoding);
    //        int endLine = Math.Min(startLine + count, lines.Length);
    //        for (int i = startLine; i < endLine; i++)
    //        {
    //            if (!string.IsNullOrWhiteSpace(lines[i]))
    //            {
    //                result.Add(lines[i].Split(new[] { _separator }, StringSplitOptions.None));
    //            }
    //        }
    //        return result;
    //    }
    //    #endregion
    //    #region 写入方法
    //    /// <summary>
    //    /// 写入一行数据
    //    /// </summary>
    //    public void WriteLine(params string[] fields)
    //    {
    //        WriteLine((IEnumerable<string>)fields);
    //    }
    //    /// <summary>
    //    /// 写入一行数据
    //    /// </summary>
    //    public void WriteLine(IEnumerable<string> fields)
    //    {
    //        if (fields == null)
    //            throw new ArgumentNullException(nameof(fields));

    //        string line = string.Join(_separator, fields);

    //        using (var writer = new StreamWriter(_filePath, true, _encoding))
    //        {
    //            writer.WriteLine(line);
    //        }
    //    }
    //    /// <summary>
    //    /// 写入多行数据
    //    /// </summary>
    //    public void WriteLines(IEnumerable<IEnumerable<string>> rows)
    //    {
    //        if (rows == null)
    //            throw new ArgumentNullException(nameof(rows));
    //        using (var writer = new StreamWriter(_filePath, true, _encoding))
    //        {
    //            foreach (var row in rows)
    //            {
    //                string line = string.Join(_separator, row);
    //                writer.WriteLine(line);
    //            }
    //        }
    //    }
    //    /// <summary>
    //    /// 写入所有行（覆盖原有内容）
    //    /// </summary>
    //    public void WriteAll(IEnumerable<IEnumerable<string>> rows)
    //    {
    //        if (rows == null)
    //            throw new ArgumentNullException(nameof(rows));
    //        using (var writer = new StreamWriter(_filePath, false, _encoding))
    //        {
    //            foreach (var row in rows)
    //            {
    //                string line = string.Join(_separator, row);
    //                writer.WriteLine(line);
    //            }
    //        }
    //    }
    //    /// <summary>
    //    /// 写入带标题的数据
    //    /// </summary>
    //    public void WriteAllWithHeaders(IEnumerable<string> headers, IEnumerable<IEnumerable<string>> rows)
    //    {
    //        if (headers == null)
    //            throw new ArgumentNullException(nameof(headers));
    //        if (rows == null)
    //            throw new ArgumentNullException(nameof(rows));
    //        using (var writer = new StreamWriter(_filePath, false, _encoding))
    //        {
    //            // 写入标题行
    //            writer.WriteLine(string.Join(_separator, headers));
    //            // 写入数据行
    //            foreach (var row in rows)
    //            {
    //                writer.WriteLine(string.Join(_separator, row));
    //            }
    //        }
    //    }
    //    #endregion
    //    #region 追加方法
    //    /// <summary>
    //    /// 追加一行数据
    //    /// </summary>
    //    public void AppendLine(params string[] fields)
    //    {
    //        WriteLine(fields);
    //    }
    //    /// <summary>
    //    /// 追加多行数据
    //    /// </summary>
    //    public void AppendLines(IEnumerable<IEnumerable<string>> rows)
    //    {
    //        WriteLines(rows);
    //    }
    //    #endregion
    //    #region 更新方法
    //    /// <summary>
    //    /// 更新指定行的数据
    //    /// </summary>
    //    public void UpdateLine(int lineNumber, params string[] newFields)
    //    {
    //        UpdateLine(lineNumber, (IEnumerable<string>)newFields);
    //    }
    //    /// <summary>
    //    /// 更新指定行的数据
    //    /// </summary>
    //    public void UpdateLine(int lineNumber, IEnumerable<string> newFields)
    //    {
    //        if (lineNumber < 0)
    //            throw new ArgumentOutOfRangeException(nameof(lineNumber), "行号不能为负数");
    //        var lines = File.ReadAllLines(_filePath, _encoding).ToList();
    //        if (lineNumber >= lines.Count)
    //            throw new IndexOutOfRangeException($"行号 {lineNumber} 超出范围");
    //        lines[lineNumber] = string.Join(_separator, newFields);
    //        File.WriteAllLines(_filePath, lines, _encoding);
    //    }
    //    /// <summary>
    //    /// 更新指定字段的值
    //    /// </summary>
    //    public void UpdateField(int lineNumber, int fieldIndex, string newValue)
    //    {
    //        var lines = File.ReadAllLines(_filePath, _encoding).ToList();
    //        if (lineNumber >= lines.Count)
    //            throw new IndexOutOfRangeException($"行号 {lineNumber} 超出范围");
    //        var fields = lines[lineNumber].Split(new[] { _separator }, StringSplitOptions.None);
    //        if (fieldIndex >= fields.Length)
    //            throw new IndexOutOfRangeException($"字段索引 {fieldIndex} 超出范围");
    //        fields[fieldIndex] = newValue;
    //        lines[lineNumber] = string.Join(_separator, fields);
    //        File.WriteAllLines(_filePath, lines, _encoding);
    //    }
    //    #endregion
    //    #region 删除方法
    //    /// <summary>
    //    /// 删除指定行
    //    /// </summary>
    //    public void DeleteLine(int lineNumber)
    //    {
    //        if (lineNumber < 0)
    //            throw new ArgumentOutOfRangeException(nameof(lineNumber), "行号不能为负数");
    //        var lines = File.ReadAllLines(_filePath, _encoding).ToList();
    //        if (lineNumber >= lines.Count)
    //            throw new IndexOutOfRangeException($"行号 {lineNumber} 超出范围");
    //        lines.RemoveAt(lineNumber);
    //        File.WriteAllLines(_filePath, lines, _encoding);
    //    }
    //    /// <summary>
    //    /// 清空所有内容
    //    /// </summary>
    //    public void Clear()
    //    {
    //        File.WriteAllText(_filePath, string.Empty, _encoding);
    //    }
    //    #endregion
    //    #region 辅助方法
    //    /// <summary>
    //    /// 获取行数
    //    /// </summary>
    //    public int GetLineCount()
    //    {
    //        if (!File.Exists(_filePath))
    //            return 0;
    //        return File.ReadAllLines(_filePath, _encoding).Length;
    //    }
    //    /// <summary>
    //    /// 检查文件是否存在
    //    /// </summary>
    //    public bool Exists()
    //    {
    //        return File.Exists(_filePath);
    //    }
    //    /// <summary>
    //    /// 删除文件
    //    /// </summary>
    //    public void DeleteFile()
    //    {
    //        if (File.Exists(_filePath))
    //        {
    //            File.Delete(_filePath);
    //        }
    //    }
    //    #endregion
    //}
    /// <summary>
    /// 简单的CSV读写辅助类 (不考虑带引号包含换行/逗号的复杂转义)
    /// </summary>
    public class CsvHelper
    {
        private readonly string _filePath;
        private readonly string _separator;
        // 强制写入时使用的编码 (带 BOM 的 UTF8，防止 Excel 乱码)
        private readonly Encoding _writeEncoding = new UTF8Encoding(true);
        /// <summary>
        /// 初始化CsvHelper
        /// </summary>
        /// <param name="filePath">CSV文件路径</param>
        /// <param name="separator">分隔符（默认逗号）</param>
        public CsvHelper(string filePath, string separator = ",")
        {
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            _separator = separator ?? ",";
        }
        /// <summary>
        /// 动态获取当前文件的读取编码
        /// </summary>
        private Encoding GetReadEncoding()
        {
            return EncodingHelper.DetectEncoding(_filePath);
        }

        #region 读取方法

        /// <summary>
        /// 读取所有行，返回字符串列表（每行是一个字符串数组）
        /// </summary>
        public List<string[]> ReadAll()
        {
            var result = new List<string[]>();
            if (!File.Exists(_filePath)) return result;

            using (var reader = new StreamReader(_filePath, GetReadEncoding()))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var fields = line.Split(new[] { _separator }, StringSplitOptions.None);
                    result.Add(fields);
                }
            }
            return result;
        }
        /// <summary>
        /// 读取所有行，返回List<Dictionary<string, string>>（使用第一行作为标题）
        /// </summary>
        public List<Dictionary<string, string>> ReadAllWithHeaders()
        {
            var result = new List<Dictionary<string, string>>();
            if (!File.Exists(_filePath)) return result;

            var lines = File.ReadAllLines(_filePath, GetReadEncoding());
            if (lines.Length < 2) return result;

            var headers = lines[0].Split(new[] { _separator }, StringSplitOptions.None);

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                var fields = lines[i].Split(new[] { _separator }, StringSplitOptions.None);
                var dict = new Dictionary<string, string>();
                for (int j = 0; j < headers.Length && j < fields.Length; j++)
                {
                    dict[headers[j].Trim()] = fields[j];
                }
                result.Add(dict);
            }
            return result;
        }
        /// <summary>
        /// 读取指定行
        /// </summary>
        public string[] ReadLine(int lineNumber)
        {
            if (lineNumber < 0) throw new ArgumentOutOfRangeException(nameof(lineNumber), "行号不能为负数");
            if (!File.Exists(_filePath)) return null;

            var lines = File.ReadAllLines(_filePath, GetReadEncoding());
            if (lineNumber >= lines.Length) return null;

            return lines[lineNumber].Split(new[] { _separator }, StringSplitOptions.None);
        }
        /// <summary>
        /// 读取指定范围的行的字段值
        /// </summary>
        public List<string[]> ReadRange(int startLine, int count)
        {
            var result = new List<string[]>();
            if (!File.Exists(_filePath)) return result;

            var lines = File.ReadAllLines(_filePath, GetReadEncoding());
            int endLine = Math.Min(startLine + count, lines.Length);

            for (int i = startLine; i < endLine; i++)
            {
                if (!string.IsNullOrWhiteSpace(lines[i]))
                {
                    result.Add(lines[i].Split(new[] { _separator }, StringSplitOptions.None));
                }
            }
            return result;
        }
        #endregion

        #region 写入方法 (覆盖/追加)
        /// <summary>
        /// 追加一行数据
        /// </summary>
        public void WriteLine(params string[] fields)
        {
            WriteLine((IEnumerable<string>)fields);
        }
        /// <summary>
        /// 追加一行数据
        /// </summary>
        public void WriteLine(IEnumerable<string> fields)
        {
            if (fields == null) throw new ArgumentNullException(nameof(fields));
            string line = string.Join(_separator, fields);
            // true 代表追加
            using (var writer = new StreamWriter(_filePath, true, _writeEncoding))
            {
                writer.WriteLine(line);
            }
        }
        /// <summary>
        /// 追加多行数据
        /// </summary>
        public void WriteLines(IEnumerable<IEnumerable<string>> rows)
        {
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            using (var writer = new StreamWriter(_filePath, true, _writeEncoding))
            {
                foreach (var row in rows)
                {
                    writer.WriteLine(string.Join(_separator, row));
                }
            }
        }
        /// <summary>
        /// 写入所有行（清空并覆盖原有内容）
        /// </summary>
        public void WriteAll(IEnumerable<IEnumerable<string>> rows)
        {
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            using (var writer = new StreamWriter(_filePath, false, _writeEncoding))
            {
                foreach (var row in rows)
                {
                    writer.WriteLine(string.Join(_separator, row));
                }
            }
        }
        /// <summary>
        /// 写入带标题的数据（覆盖原有内容）
        /// </summary>
        public void WriteAllWithHeaders(IEnumerable<string> headers, IEnumerable<IEnumerable<string>> rows)
        {
            if (headers == null) throw new ArgumentNullException(nameof(headers));
            if (rows == null) throw new ArgumentNullException(nameof(rows));

            using (var writer = new StreamWriter(_filePath, false, _writeEncoding))
            {
                writer.WriteLine(string.Join(_separator, headers));
                foreach (var row in rows)
                {
                    writer.WriteLine(string.Join(_separator, row));
                }
            }
        }
        #endregion

        #region 追加方法的别名映射
        public void AppendLine(params string[] fields) => WriteLine(fields);
        public void AppendLines(IEnumerable<IEnumerable<string>> rows) => WriteLines(rows);
        #endregion

        #region 更新与删除方法

        /// <summary>
        /// 更新指定行的数据
        /// </summary>
        public void UpdateLine(int lineNumber, params string[] newFields)
        {
            UpdateLine(lineNumber, (IEnumerable<string>)newFields);
        }
        /// <summary>
        /// 更新指定行的数据
        /// </summary>
        public void UpdateLine(int lineNumber, IEnumerable<string> newFields)
        {
            if (lineNumber < 0) throw new ArgumentOutOfRangeException(nameof(lineNumber), "行号不能为负数");
            if (!File.Exists(_filePath)) return;

            // 1. 原编码读取
            var lines = File.ReadAllLines(_filePath, GetReadEncoding()).ToList();
            if (lineNumber >= lines.Count) throw new IndexOutOfRangeException($"行号 {lineNumber} 超出范围");

            // 2. 更新内存数据
            lines[lineNumber] = string.Join(_separator, newFields);

            // 3. 强制使用 UTF-8 覆写回文件
            File.WriteAllLines(_filePath, lines, _writeEncoding);
        }
        /// <summary>
        /// 更新指定字段的值
        /// </summary>
        public void UpdateField(int lineNumber, int fieldIndex, string newValue)
        {
            if (!File.Exists(_filePath)) return;

            var lines = File.ReadAllLines(_filePath, GetReadEncoding()).ToList();
            if (lineNumber >= lines.Count) throw new IndexOutOfRangeException($"行号 {lineNumber} 超出范围");

            var fields = lines[lineNumber].Split(new[] { _separator }, StringSplitOptions.None);
            if (fieldIndex >= fields.Length) throw new IndexOutOfRangeException($"字段索引 {fieldIndex} 超出范围");

            fields[fieldIndex] = newValue;
            lines[lineNumber] = string.Join(_separator, fields);

            File.WriteAllLines(_filePath, lines, _writeEncoding);
        }
        /// <summary>
        /// 删除指定行
        /// </summary>
        public void DeleteLine(int lineNumber)
        {
            if (lineNumber < 0) throw new ArgumentOutOfRangeException(nameof(lineNumber), "行号不能为负数");
            if (!File.Exists(_filePath)) return;

            var lines = File.ReadAllLines(_filePath, GetReadEncoding()).ToList();
            if (lineNumber >= lines.Count) throw new IndexOutOfRangeException($"行号 {lineNumber} 超出范围");

            lines.RemoveAt(lineNumber);

            File.WriteAllLines(_filePath, lines, _writeEncoding);
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 获取行数 (流式读取，防内存溢出)
        /// </summary>
        public int GetLineCount()
        {
            if (!File.Exists(_filePath)) return 0;

            int count = 0;
            using (var reader = new StreamReader(_filePath, GetReadEncoding()))
            {
                while (reader.ReadLine() != null)
                {
                    count++;
                }
            }
            return count;
        }
        public void Clear()
        {
            File.WriteAllText(_filePath, string.Empty, _writeEncoding);
        }
        public bool Exists() => File.Exists(_filePath);
        public void DeleteFile()
        {
            if (File.Exists(_filePath)) File.Delete(_filePath);
        }
        #endregion
    }
    /// <summary>
    /// 泛型CSV帮助类，支持对象映射
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    //public class CsvHelper<T> where T : class, new()
    //{
    //    private readonly string _filePath;
    //    private readonly Encoding _encoding;
    //    private readonly string _separator;
    //    private readonly PropertyInfo[] _properties;
    //    public CsvHelper(string filePath, Encoding encoding = null, string separator = ",")
    //    {
    //        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    //        _encoding = encoding ?? Encoding.UTF8;
    //        _separator = separator ?? ",";
    //        _properties = typeof(T).GetProperties();
    //    }
    //    #region 读取方法
    //    /// <summary>
    //    /// 读取所有行并转换为对象列表（使用第一行作为标题）
    //    /// </summary>
    //    public List<T> ReadAll()
    //    {
    //        var result = new List<T>();

    //        if (!File.Exists(_filePath))
    //            return result;

    //        var lines = File.ReadAllLines(_filePath, _encoding);
    //        if (lines.Length < 2)
    //            return result;

    //        // 解析标题行
    //        var headers = lines[0].Split(new[] { _separator }, StringSplitOptions.None);

    //        // 创建属性名到索引的映射
    //        var propertyMap = new Dictionary<string, int>();
    //        for (int i = 0; i < headers.Length; i++)
    //        {
    //            propertyMap[headers[i].Trim()] = i;
    //        }

    //        // 读取数据行
    //        for (int i = 1; i < lines.Length; i++)
    //        {
    //            if (string.IsNullOrWhiteSpace(lines[i])) continue;

    //            var fields = lines[i].Split(new[] { _separator }, StringSplitOptions.None);
    //            var obj = new T();

    //            foreach (var prop in _properties)
    //            {
    //                if (propertyMap.TryGetValue(prop.Name, out int index) && index < fields.Length)
    //                {
    //                    SetPropertyValue(obj, prop, fields[index]);
    //                }
    //            }
    //            result.Add(obj);
    //        }
    //        return result;
    //    }

    //    /// <summary>
    //    /// 读取所有行并转换为对象列表（使用自定义标题映射）
    //    /// </summary>
    //    public List<T> ReadAll(Dictionary<string, string> headerMapping)
    //    {
    //        var result = new List<T>();
    //        if (!File.Exists(_filePath))
    //            return result;
    //        var lines = File.ReadAllLines(_filePath, _encoding);
    //        if (lines.Length < 2)
    //            return result;
    //        // 解析标题行
    //        var headers = lines[0].Split(new[] { _separator }, StringSplitOptions.None);

    //        // 创建属性名到索引的映射（使用映射关系）
    //        var propertyMap = new Dictionary<string, int>();
    //        foreach (var mapping in headerMapping)
    //        {
    //            for (int i = 0; i < headers.Length; i++)
    //            {
    //                if (headers[i].Trim() == mapping.Value)
    //                {
    //                    propertyMap[mapping.Key] = i;
    //                    break;
    //                }
    //            }
    //        }
    //        // 读取数据行
    //        for (int i = 1; i < lines.Length; i++)
    //        {
    //            if (string.IsNullOrWhiteSpace(lines[i]))
    //                continue;

    //            var fields = lines[i].Split(new[] { _separator }, StringSplitOptions.None);
    //            var obj = new T();

    //            foreach (var prop in _properties)
    //            {
    //                if (propertyMap.TryGetValue(prop.Name, out int index) && index < fields.Length)
    //                {
    //                    SetPropertyValue(obj, prop, fields[index]);
    //                }
    //            }

    //            result.Add(obj);
    //        }

    //        return result;
    //    }
    //    #endregion

    //    #region 写入方法
    //    /// <summary>
    //    /// 写入对象列表（包含标题行）
    //    /// </summary>
    //    public void WriteAll(IEnumerable<T> items)
    //    {
    //        if (items == null)
    //            throw new ArgumentNullException(nameof(items));

    //        using (var writer = new StreamWriter(_filePath, false, _encoding))
    //        {
    //            // 写入标题行（使用属性名）
    //            var headers = _properties.Select(p => p.Name).ToArray();
    //            writer.WriteLine(string.Join(_separator, headers));

    //            // 写入数据行
    //            foreach (var item in items)
    //            {
    //                var values = _properties.Select(p => GetPropertyValue(item, p)?.ToString() ?? "").ToArray();
    //                writer.WriteLine(string.Join(_separator, values));
    //            }
    //        }
    //    }
    //    /// <summary>
    //    /// 写入对象列表（使用自定义标题）
    //    /// </summary>
    //    public void WriteAll(IEnumerable<T> items, Dictionary<string, string> headerMapping)
    //    {
    //        if (items == null)
    //            throw new ArgumentNullException(nameof(items));
    //        if (headerMapping == null)
    //            throw new ArgumentNullException(nameof(headerMapping));

    //        using (var writer = new StreamWriter(_filePath, false, _encoding))
    //        {
    //            // 写入标题行（使用映射的值）
    //            var orderedProps = headerMapping.Keys.ToList();
    //            var headers = orderedProps.Select(p => headerMapping[p]).ToArray();
    //            writer.WriteLine(string.Join(_separator, headers));

    //            // 写入数据行
    //            foreach (var item in items)
    //            {
    //                var values = orderedProps.Select(propName =>
    //                {
    //                    var prop = _properties.FirstOrDefault(p => p.Name == propName);
    //                    return prop != null ? (GetPropertyValue(item, prop)?.ToString() ?? "") : "";
    //                }).ToArray();

    //                writer.WriteLine(string.Join(_separator, values));
    //            }
    //        }
    //    }
    //    /// <summary>
    //    /// 追加对象
    //    /// </summary>
    //    public void Append(T item)
    //    {
    //        AppendAll(new[] { item });
    //    }
    //    /// <summary>
    //    /// 追加对象列表
    //    /// </summary>
    //    public void AppendAll(IEnumerable<T> items)
    //    {
    //        if (items == null)
    //            throw new ArgumentNullException(nameof(items));

    //        bool fileExists = File.Exists(_filePath);

    //        using (var writer = new StreamWriter(_filePath, true, _encoding))
    //        {
    //            // 如果文件不存在且没有标题行，则先写入标题
    //            if (!fileExists)
    //            {
    //                var headers = _properties.Select(p => p.Name).ToArray();
    //                writer.WriteLine(string.Join(_separator, headers));
    //            }

    //            // 写入数据行
    //            foreach (var item in items)
    //            {
    //                var values = _properties.Select(p => GetPropertyValue(item, p)?.ToString() ?? "").ToArray();
    //                writer.WriteLine(string.Join(_separator, values));
    //            }
    //        }
    //    }
    //    #endregion

    //    #region 辅助方法
    //    private void SetPropertyValue(T obj, PropertyInfo prop, string value)
    //    {
    //        try
    //        {
    //            if (prop.PropertyType == typeof(string))
    //            {
    //                prop.SetValue(obj, value);
    //            }
    //            else if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(int?))
    //            {
    //                if (int.TryParse(value, out int intValue))
    //                    prop.SetValue(obj, intValue);
    //            }
    //            else if (prop.PropertyType == typeof(double) || prop.PropertyType == typeof(double?))
    //            {
    //                if (double.TryParse(value, out double doubleValue))
    //                    prop.SetValue(obj, doubleValue);
    //            }
    //            else if (prop.PropertyType == typeof(decimal) || prop.PropertyType == typeof(decimal?))
    //            {
    //                if (decimal.TryParse(value, out decimal decimalValue))
    //                    prop.SetValue(obj, decimalValue);
    //            }
    //            else if (prop.PropertyType == typeof(bool) || prop.PropertyType == typeof(bool?))
    //            {
    //                if (bool.TryParse(value, out bool boolValue))
    //                    prop.SetValue(obj, boolValue);
    //            }
    //            else if (prop.PropertyType == typeof(DateTime) || prop.PropertyType == typeof(DateTime?))
    //            {
    //                if (DateTime.TryParse(value, out DateTime dateValue))
    //                    prop.SetValue(obj, dateValue);
    //            }
    //        }
    //        catch
    //        {
    //            // 忽略转换错误
    //        }
    //    }
    //    private object GetPropertyValue(T obj, PropertyInfo prop)
    //    {
    //        return prop.GetValue(obj);
    //    }
    //    #endregion
    //}
}
