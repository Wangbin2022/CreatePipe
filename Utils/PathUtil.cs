namespace CreatePipe.utils
{
    /// <summary>
    /// 路径工具类
    /// </summary>
    public static class PathUtil
    {
        /// <summary>
        /// 获取当前dll的路径  完整的 D:\revitpro\RevitPro\RevitPro\bin\Debug\RevitPro.dll
        /// </summary>
        /// <returns></returns>
        public static string GetCurrentDllAllPath()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().Location;
        }
        /// <summary>
        /// 获取当前dll的所在目录
        /// </summary>
        /// <returns></returns>
        public static string GetCurrentDllPathDirectory()
        {
            string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            return path;
        }
    }
}
