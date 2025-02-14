using System;
using System.Runtime.InteropServices;

namespace CreatePipe.Utils
{
    public static class WindowHelper
    {        /// <summary>
             /// 找到窗口
             /// </summary>
             /// <param name="lpClassName">窗口类名(例：Button)</param>
             /// <param name="lpWindowName">窗口标题</param>
             /// <returns></returns>
        [DllImport("user32.dll", EntryPoint = "FindWindow")]
        public extern static IntPtr FindWindow(string lpClassName, string lpWindowName);

        /// <summary>
        /// 找到窗口
        /// </summary>
        /// <param name="hwndParent">父窗口句柄（如果为空，则为桌面窗口）</param>
        /// <param name="hwndChildAfter">子窗口句柄（从该子窗口之后查找）</param>
        /// <param name="lpszClass">窗口类名(例：Button</param>
        /// <param name="lpszWindow">窗口标题</param>
        /// <returns></returns>
        [DllImport("user32.dll", EntryPoint = "FindWindowEx")]
        public extern static IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll", EntryPoint = "CloseWindow")]
        public static extern bool CloseWindow(IntPtr hwnd);

        [DllImport("user32.dll", EntryPoint = "PostMessage")]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="hwnd">消息接受窗口句柄</param>
        /// <param name="wMsg">消息</param>
        /// <param name="wParam">指定附加的消息特定信息</param>
        /// <param name="lParam">指定附加的消息特定信息</param>
        /// <returns></returns>
        [DllImport("user32.dll", EntryPoint = "SendMessageA")]

        private static extern int SendMessage(IntPtr hwnd, uint wMsg, int wParam, int lParam);

        //窗口发送给按钮控件的消息，让按钮执行点击操作，可以模拟按钮点击
        private const int BM_CLICK = 0xF5;
        public const uint WM_CLOSE = 0x0010;
    }
}
