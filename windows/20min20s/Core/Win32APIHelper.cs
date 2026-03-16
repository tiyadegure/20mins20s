using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace ProjectEye.Core
{
    public class Win32APIHelper
    {
        private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

        /// <summary>
        /// 获取鼠标坐标
        /// </summary>
        /// <param name="lpPoint"></param>
        /// <returns></returns>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCursorPos(out Point lpPoint);

        #region 窗口类
        /// <summary>
        /// 获取窗口标题
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="lpString"></param>
        /// <param name="nMaxCount"></param>
        /// <returns></returns>
        [DllImport("user32", SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        /// <summary>
        /// 获取窗口类名
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="lpString"></param>
        /// <param name="nMaxCount"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        /// <summary>
        /// 获取当前焦点窗口句柄
        /// </summary>
        /// <returns></returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern IntPtr GetForegroundWindow();
        /// <summary>
        /// 获取窗口所属进程
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="processId"></param>
        /// <returns></returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
        /// <summary>
        /// 窗口是否最大化
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        public static extern bool IsZoomed(IntPtr hWnd);
        /// <summary>
        /// 获取窗口位置
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="lpRect"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);
        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        /// <summary>
        /// 窗口信息结构
        /// </summary>
        public struct WindowInfo
        {
            public int Width;
            public int Height;
            public string Title;
            public string ClassName;
            public bool IsFullScreen;
            public bool IsZoomed;
            public uint ProcessId;
            public string ProcessName;
        }

        /// <summary>
        /// 获取当前焦点窗口信息
        /// </summary>
        /// <returns></returns>
        public static WindowInfo GetFocusWindowInfo()
        {
            WindowInfo result = new WindowInfo();
            IntPtr intPtr = GetForegroundWindow();

            RECT rect = new RECT();
            GetWindowRect(intPtr, ref rect);
            result.IsZoomed = IsZoomed(intPtr);
            result.Width = rect.Right - rect.Left;
            result.Height = rect.Bottom - rect.Top;

            StringBuilder title = new StringBuilder(256);
            GetWindowText(intPtr, title, title.Capacity);
            result.Title = title.ToString();

            StringBuilder className = new StringBuilder(256);
            GetClassName(intPtr, className, className.Capacity);
            result.ClassName = className.ToString();

            GetWindowThreadProcessId(intPtr, out uint processId);
            result.ProcessId = processId;
            result.ProcessName = GetProcessName(processId);
            result.IsFullScreen = IsForegroundWindowFullScreen(intPtr, rect, result.ClassName);

            return result;
        }

        public static TimeSpan GetIdleTime()
        {
            LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(typeof(LASTINPUTINFO));
            if (!GetLastInputInfo(ref lastInputInfo))
            {
                return TimeSpan.Zero;
            }

            uint tickCount = unchecked((uint)Environment.TickCount);
            uint idleTicks = tickCount - lastInputInfo.dwTime;
            return TimeSpan.FromMilliseconds(idleTicks);
        }

        private static string GetProcessName(uint processId)
        {
            if (processId == 0)
            {
                return string.Empty;
            }

            try
            {
                return Process.GetProcessById((int)processId).ProcessName;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static bool IsForegroundWindowFullScreen(IntPtr hwnd, RECT windowRect, string className)
        {
            if (hwnd == IntPtr.Zero)
            {
                return false;
            }

            string lowerClassName = (className ?? string.Empty).ToLowerInvariant();
            if (lowerClassName == "progman" || lowerClassName == "workerw" || lowerClassName == "shell_traywnd")
            {
                return false;
            }

            IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            if (monitor == IntPtr.Zero)
            {
                return false;
            }

            MONITORINFO monitorInfo = new MONITORINFO();
            monitorInfo.cbSize = Marshal.SizeOf(typeof(MONITORINFO));
            if (!GetMonitorInfo(monitor, ref monitorInfo))
            {
                return false;
            }

            const int tolerance = 2;
            return
                Math.Abs(windowRect.Left - monitorInfo.rcMonitor.Left) <= tolerance &&
                Math.Abs(windowRect.Top - monitorInfo.rcMonitor.Top) <= tolerance &&
                Math.Abs(windowRect.Right - monitorInfo.rcMonitor.Right) <= tolerance &&
                Math.Abs(windowRect.Bottom - monitorInfo.rcMonitor.Bottom) <= tolerance;
        }
        #endregion

        #region 获取系统信息
        [DllImport("ntdll.dll", SetLastError = true)]
        internal static extern uint RtlGetVersion(out OsVersionInfo versionInformation);

        [StructLayout(LayoutKind.Sequential)]
        internal struct OsVersionInfo
        {
            private readonly uint OsVersionInfoSize;
            internal readonly uint MajorVersion;
            internal readonly uint MinorVersion;
            private readonly uint BuildNumber;
            private readonly uint PlatformId;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            private readonly string CSDVersion;
        }
        #endregion
    }
}
