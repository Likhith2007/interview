using System;
using System.Runtime.InteropServices;
using System.Text;

namespace OverlayDetector
{
    /// <summary>
    /// P/Invoke declarations for Win32 API window functions.
    /// </summary>
    public static class Win32NativeMethods
    {
        // Window style flags
        public const uint WS_EX_TOPMOST = 0x00000008;
        public const uint WS_EX_NOACTIVATE = 0x08000000;

        // Delegate for EnumWindows callback
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        /// <summary>
        /// Enumerates all top-level windows on the screen by passing the handle to each window, 
        /// in turn, to an application-defined callback function.
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        /// <summary>
        /// Retrieves a handle to the foreground window (the window with which the user is currently working).
        /// </summary>
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        /// <summary>
        /// Retrieves the extended window style of the specified window.
        /// </summary>
        [DllImport("user32.dll")]
        public static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

        /// <summary>
        /// Determines the visibility state of the specified window.
        /// </summary>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        /// <summary>
        /// Determines whether a window is a child window or pop-up window.
        /// </summary>
        [DllImport("user32.dll")]
        public static extern IntPtr GetParent(IntPtr hWnd);

        /// <summary>
        /// Copies the text of the specified window's title bar into a buffer.
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        /// <summary>
        /// Retrieves the length of the specified window's title bar text (in characters).
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        /// <summary>
        /// Retrieves the name of the class to which the specified window belongs.
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        /// <summary>
        /// Retrieves the process ID (PID) associated with the specified window.
        /// </summary>
        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        /// <summary>
        /// Retrieves a handle to the desktop window.
        /// </summary>
        [DllImport("user32.dll")]
        public static extern IntPtr GetDesktopWindow();

        // Window display state constants
        public const int GWL_EXSTYLE = -20;

        /// <summary>
        /// Gets the window title safely.
        /// </summary>
        public static string GetWindowTitle(IntPtr hWnd)
        {
            int length = GetWindowTextLength(hWnd);
            if (length == 0)
                return string.Empty;

            StringBuilder sb = new StringBuilder(length + 1);
            GetWindowText(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }

        /// <summary>
        /// Gets the window class name safely.
        /// </summary>
        public static string GetWindowClassName(IntPtr hWnd)
        {
            StringBuilder sb = new StringBuilder(256);
            GetClassName(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }

        /// <summary>
        /// Gets the process name for a window handle.
        /// </summary>
        public static string GetProcessName(IntPtr hWnd)
        {
            try
            {
                GetWindowThreadProcessId(hWnd, out uint processId);
                var process = System.Diagnostics.Process.GetProcessById((int)processId);
                return process.ProcessName;
            }
            catch
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// Checks if a window has the TOPMOST extended style.
        /// </summary>
        public static bool IsTopMostWindow(IntPtr hWnd)
        {
            uint exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
            return (exStyle & WS_EX_TOPMOST) != 0;
        }
    }
}
