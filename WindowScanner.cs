using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace OverlayDetector
{
    /// <summary>
    /// Represents a detected overlay window.
    /// </summary>
    public class OverlayWindow
    {
        public string ProcessName { get; set; } = string.Empty;
        public string WindowTitle { get; set; } = string.Empty;
        public string WindowClass { get; set; } = string.Empty;
        public DateTime DetectedAt { get; set; }
        public IntPtr Handle { get; set; }

        public override string ToString() => $"{ProcessName} - {WindowTitle} [{DetectedAt:HH:mm:ss}]";
    }

    /// <summary>
    /// Monitors windows and detects overlays based on WS_EX_TOPMOST style and other patterns.
    /// </summary>
    public class WindowScanner
    {
        // Whitelist of known video conferencing and system applications
        private static readonly HashSet<string> WhitelistedProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "teams",
            "msedge",
            "chrome",
            "firefox",
            "zoom",
            "skype",
            "webex",
            "googlemeet",
            "meet",
            "jitsi",
            "discord",  // Changed: discord is chat app, often has legitimate overlays
            "slack",    // Changed: same as above
            "outlook",
            "mail",
            "notes",
            "calc",
            "vlc",
            "ffmpeg",
            "dwm",                                              // Desktop Window Manager
            "explorer",
            "svchost",
            "lsass",
            "csrss",
            "services",
            "searchindexer",
            "applicationframehost",
            "shellexperiencehost",
            "textinputhost",
            "taskmgr",
            "taskhostw",
            "serviceshostprocess",
            "amd",
            "nvidia",
            "intel",
            "displaylink",
            "obs",
            "studio",
            "elgato",
            "streamlabs"
        };

        // Common system window classes that should be ignored
        private static readonly HashSet<string> SystemWindowClasses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Shell_TrayWnd",
            "Shell_SecondaryTrayWnd",
            "Progman",
            "WorkerW",
            "ClipboardListenerWindow",
            "IME",
            "IMECLASSLIST",
            "Static",
            "Button",
            "Edit"
        };

        private List<OverlayWindow> _detectedOverlays = new();
        private HashSet<IntPtr> _previouslyDetectedHandles = new();
        public bool DebugMode { get; set; } = true; // Enable debug output
        public List<string> DebugLog { get; private set; } = new();

        public event EventHandler<OverlayWindow>? OverlayDetected;
        public event EventHandler<List<OverlayWindow>>? OverlaysUpdated;

        private void LogDebug(string message)
        {
            if (DebugMode)
            {
                DebugLog.Add($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
                System.Diagnostics.Debug.WriteLine(message);
                Console.WriteLine(message);
            }
        }

        /// <summary>
        /// Performs a single scan of all top-level windows and detects overlays.
        /// </summary>
        public void ScanForOverlays()
        {
            var currentOverlays = new List<OverlayWindow>();
            var currentHandles = new HashSet<IntPtr>();
            int windowsScanned = 0;

            LogDebug("=== STARTING WINDOW SCAN ===");

            // Enumerate all top-level windows
            Win32NativeMethods.EnumWindows((hWnd, lParam) =>
            {
                windowsScanned++;
                try
                {
                    string processName = Win32NativeMethods.GetProcessName(hWnd);
                    string windowTitle = Win32NativeMethods.GetWindowTitle(hWnd);
                    string windowClass = Win32NativeMethods.GetWindowClassName(hWnd);
                    bool isVisible = Win32NativeMethods.IsWindowVisible(hWnd);
                    bool isTopmost = Win32NativeMethods.IsTopMostWindow(hWnd);

                    // Only log non-system windows
                    if (isVisible && !string.IsNullOrWhiteSpace(windowTitle))
                    {
                        LogDebug($"Window: {processName} | Title: '{windowTitle}' | Class: {windowClass} | Topmost: {isTopmost}");
                    }

                    if (IsRelevantWindow(hWnd))
                    {
                        if (IsOverlay(hWnd) && !IsWhitelisted(hWnd))
                        {
                            var overlay = new OverlayWindow
                            {
                                Handle = hWnd,
                                ProcessName = processName,
                                WindowTitle = windowTitle,
                                WindowClass = windowClass,
                                DetectedAt = DateTime.Now
                            };

                            LogDebug($"✓ OVERLAY DETECTED: {processName} - {windowTitle}");

                            currentOverlays.Add(overlay);
                            currentHandles.Add(hWnd);

                            // Raise event only for newly detected overlays
                            if (!_previouslyDetectedHandles.Contains(hWnd))
                            {
                                OverlayDetected?.Invoke(this, overlay);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogDebug($"Error scanning window {hWnd}: {ex.Message}");
                }

                return true; // Continue enumeration
            }, IntPtr.Zero);

            LogDebug($"=== SCAN COMPLETE: {windowsScanned} windows scanned, {currentOverlays.Count} overlays found ===");

            _detectedOverlays = currentOverlays;
            _previouslyDetectedHandles = currentHandles;

            // Raise update event
            OverlaysUpdated?.Invoke(this, currentOverlays);
        }

        /// <summary>
        /// Gets the current list of detected overlays.
        /// </summary>
        public List<OverlayWindow> GetDetectedOverlays() => new List<OverlayWindow>(_detectedOverlays);

        /// <summary>
        /// Determines if a window should be considered for overlay detection.
        /// </summary>
        private bool IsRelevantWindow(IntPtr hWnd)
        {
            // Only consider visible windows
            if (!Win32NativeMethods.IsWindowVisible(hWnd))
                return false;

            // Exclude child windows (some child windows are actually overlays)
            // Only exclude windows with a parent that is also a top-level window
            IntPtr parent = Win32NativeMethods.GetParent(hWnd);
            if (parent != IntPtr.Zero && parent != Win32NativeMethods.GetDesktopWindow())
                return false;

            string windowClass = Win32NativeMethods.GetWindowClassName(hWnd);
            
            // Skip system window classes
            if (SystemWindowClasses.Contains(windowClass))
                return false;

            // Skip windows with empty titles (usually system windows)
            string title = Win32NativeMethods.GetWindowTitle(hWnd);
            if (string.IsNullOrWhiteSpace(title))
                return false;

            return true;
        }

        /// <summary>
        /// Determines if a window is an overlay based on multiple criteria.
        /// </summary>
        private bool IsOverlay(IntPtr hWnd)
        {
            // Check if window has TOPMOST extended style
            if (Win32NativeMethods.IsTopMostWindow(hWnd))
                return true;

            // Check if window has NOACTIVATE style (often used by floating windows)
            uint exStyle = Win32NativeMethods.GetWindowLong(hWnd, -20); // GWL_EXSTYLE
            if ((exStyle & Win32NativeMethods.WS_EX_NOACTIVATE) != 0)
                return true;

            // Additional check: windows that are always on top but not minimized
            // and have limited title length are likely overlays
            string title = Win32NativeMethods.GetWindowTitle(hWnd);
            string className = Win32NativeMethods.GetWindowClassName(hWnd);
            
            // Chrome/Electron overlay detection (floating windows, popups)
            if ((className.Contains("Chrome") || className.Contains("Electron")) && title.Length > 0 && title.Length < 100)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if a window is in the whitelist.
        /// </summary>
        private bool IsWhitelisted(IntPtr hWnd)
        {
            try
            {
                string processName = Win32NativeMethods.GetProcessName(hWnd);
                string windowClass = Win32NativeMethods.GetWindowClassName(hWnd);
                string windowTitle = Win32NativeMethods.GetWindowTitle(hWnd);

                // Check if process name exactly matches a whitelisted process
                if (WhitelistedProcesses.Contains(processName))
                    return true;

                // Check if process name contains whitelisted substring
                foreach (var whitelistItem in WhitelistedProcesses)
                {
                    if (processName.Contains(whitelistItem, StringComparison.OrdinalIgnoreCase))
                        return true;
                }

                // Check common system window classes
                if (windowClass.StartsWith("Shell_", StringComparison.OrdinalIgnoreCase) ||
                    windowClass == "Progman" ||
                    windowClass == "WorkerW" ||
                    windowClass == "Notification.HWND" ||
                    windowClass.StartsWith("MSTaskListWClass", StringComparison.OrdinalIgnoreCase))
                    return true;

                // Filter out Windows notification center
                if (windowTitle.Contains("Notification", StringComparison.OrdinalIgnoreCase) &&
                    windowClass.Contains("Notification", StringComparison.OrdinalIgnoreCase))
                    return true;

                return false;
            }
            catch
            {
                return true; // Whitelist on error to be safe
            }
        }

        /// <summary>
        /// Clears the list of detected overlays.
        /// </summary>
        public void ClearDetectedOverlays()
        {
            _detectedOverlays.Clear();
            _previouslyDetectedHandles.Clear();
        }
    }
}
